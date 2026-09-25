using InternetVotingApplication.Blockchain;
using InternetVotingApplication.Configuration;
using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using InternetVotingApplication.Services.Mail;
using InternetVotingApplication.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;

namespace InternetVotingApplication.Services
{
    /// <summary>
    /// Maintenance of election hash chains: full verification with a stored log, anchors (signed head snapshots
    /// sent outside the system) and the public export used for independent verification.
    /// </summary>
    public class ChainService(
        InternetVotingContext context,
        IBlockSigner signer,
        IEmailSender emailSender,
        IAuditLog auditLog,
        IOptions<ChainOptions> chainOptions,
        TimeProvider timeProvider,
        ILogger<ChainService> logger) : IChainService
    {
        public const string ReasonPeriodic = "Periodic";
        public const string ReasonElectionEnded = "ElectionEnded";
        public const string ReasonManual = "Manual";

        private readonly ChainOptions _options = chainOptions.Value;

        public async Task<ChainVerificationResult> VerifyAndStoreAsync(int electionId, string trigger, int? actorUserId = null)
        {
            var election = await context.DataWyborows.AsNoTracking().SingleOrDefaultAsync(e => e.Id == electionId)
                ?? throw new InvalidOperationException($"Election {electionId} does not exist.");

            var blocks = await context.GlosowanieWyborczes
                .AsNoTracking()
                .Where(g => g.IdWybory == electionId)
                .OrderBy(g => g.Indeks)
                .ToListAsync();

            var result = BlockChainHelper.VerifyBlockChain(blocks, signer);
            var headMatches = BlockChainHelper.HeadMatches(election, blocks.Count == 0 ? null : blocks[^1]);
            var isValid = result.IsValid && headMatches;

            var details = new StringBuilder();
            if (!result.HashesValid)
            {
                details.Append(CultureInfo.InvariantCulture, $"Niepoprawne hashe lub powiązania bloków: {string.Join(',', result.InvalidBlockIds)}. ");
            }

            if (!result.SignaturesValid)
            {
                details.Append(CultureInfo.InvariantCulture, $"Niepoprawne podpisy bloków: {string.Join(',', result.InvalidSignatureBlockIds)}. ");
            }

            if (!headMatches)
            {
                details.Append(CultureInfo.InvariantCulture, $"Stan głowy ({election.LiczbaBlokow} bloków, {election.HashGlowy}) nie zgadza się z ostatnim blokiem. ");
            }

            context.Weryfikacje.Add(new WeryfikacjaLancucha
            {
                IdWybory = electionId,
                Data = Now(),
                Poprawny = isValid,
                LiczbaBlokow = result.BlockCount,
                HashGlowy = result.HeadHash,
                Wyzwalacz = trigger,
                Szczegoly = details.Length == 0 ? null : details.ToString().TrimEnd(),
            });
            await context.SaveChangesAsync();

            if (!isValid)
            {
                logger.LogError("Chain of election {ElectionId} failed verification: {Details}", electionId, details);
                await auditLog.LogAsync(AuditLog.Actions.ChainCorrupted, $"Wybory {electionId}: {details}", actorUserId);
            }
            else if (trigger == "Manual")
            {
                await auditLog.LogAsync(AuditLog.Actions.ChainVerified, $"Wybory {electionId}: {result.BlockCount} bloków poprawnych", actorUserId);
            }

            return new ChainVerificationResult(isValid, result.BlockCount, result.HeadHash, result.InvalidBlockIds, result.InvalidSignatureBlockIds);
        }

        public Task<ChainVerificationViewModel?> GetLastVerificationAsync(int electionId)
        {
            return context.Weryfikacje
                .AsNoTracking()
                .Where(w => w.IdWybory == electionId)
                .OrderByDescending(w => w.Data)
                .ThenByDescending(w => w.Id)
                .Select(w => new ChainVerificationViewModel
                {
                    Date = w.Data,
                    IsValid = w.Poprawny,
                    BlockCount = w.LiczbaBlokow,
                    HeadHash = w.HashGlowy,
                    Trigger = w.Wyzwalacz,
                    Details = w.Szczegoly,
                })
                .FirstOrDefaultAsync();
        }

        public async Task<ChainAnchorViewModel?> PublishAnchorAsync(int electionId, string reason, int? actorUserId = null)
        {
            var election = await context.DataWyborows.AsNoTracking().SingleOrDefaultAsync(e => e.Id == electionId);
            if (election == null)
            {
                return null;
            }

            var now = Now();
            var recipients = _options.AnchorRecipients.Where(r => !string.IsNullOrWhiteSpace(r)).Select(r => r.Trim()).ToList();
            var anchor = new KotwicaLancucha
            {
                IdWybory = electionId,
                LiczbaBlokow = election.LiczbaBlokow,
                HashGlowy = election.HashGlowy,
                Data = now,
                Powod = reason,
                Odbiorcy = recipients.Count == 0 ? null : string.Join(';', recipients),
                Podpis = signer.Sign(BlockChainHelper.AnchorData(electionId, election.LiczbaBlokow, election.HashGlowy, now)),
            };
            context.Kotwice.Add(anchor);
            await context.SaveChangesAsync();

            foreach (var recipient in recipients)
            {
                await emailSender.SendAsync(ExtensionMethods.Email.ChainAnchor(recipient, election.Opis, electionId, anchor, signer.KeyId));
            }

            await auditLog.LogAsync(AuditLog.Actions.AnchorPublished,
                $"Wybory {electionId}: {anchor.LiczbaBlokow} bloków, głowa {anchor.HashGlowy ?? "(pusta)"}, powód {reason}", actorUserId);
            logger.LogInformation("Anchor published for election {ElectionId}: {Blocks} blocks, head {Head}", electionId, anchor.LiczbaBlokow, anchor.HashGlowy);

            return ToViewModel(anchor);
        }

        public async Task<ChainViewModel?> GetChainPageAsync(int electionId)
        {
            var election = await context.DataWyborows.AsNoTracking().SingleOrDefaultAsync(e => e.Id == electionId);
            if (election == null)
            {
                return null;
            }

            var anchors = await context.Kotwice
                .AsNoTracking()
                .Where(k => k.IdWybory == electionId)
                .OrderByDescending(k => k.Data)
                .ThenByDescending(k => k.Id)
                .ToListAsync();

            return new ChainViewModel
            {
                ElectionId = election.Id,
                ElectionName = election.Opis,
                DataRozpoczecia = election.DataRozpoczecia,
                DataZakonczenia = election.DataZakonczenia,
                Status = election.GetStatus(Now()),
                BlockCount = election.LiczbaBlokow,
                HeadHash = election.HashGlowy,
                KeyId = signer.KeyId,
                PublicKeyPem = signer.PublicKeyPem,
                LastVerification = await GetLastVerificationAsync(electionId),
                Anchors = anchors.Select(ToViewModel).ToList(),
            };
        }

        public async Task<ChainExport?> ExportAsync(int electionId)
        {
            var election = await context.DataWyborows.AsNoTracking().SingleOrDefaultAsync(e => e.Id == electionId);
            if (election == null)
            {
                return null;
            }

            var candidates = await context.Kandydats
                .AsNoTracking()
                .Where(k => k.IdWybory == electionId)
                .OrderBy(k => k.Id)
                .Select(k => new ChainExportCandidate(k.Id, k.Imie, k.Nazwisko))
                .ToListAsync();

            var blocks = await context.GlosowanieWyborczes
                .AsNoTracking()
                .Where(g => g.IdWybory == electionId)
                .OrderBy(g => g.Indeks)
                .ToListAsync();

            var exportBlocks = new List<ChainExportBlock>(blocks.Count);
            string? previousHash = null;
            foreach (var block in blocks)
            {
                exportBlocks.Add(new ChainExportBlock(block.Indeks, block.IdKandydat, block.IdWybory, block.ZnacznikCzasu, block.Nonce, previousHash, block.Hash, block.Podpis, block.IdKlucza));
                previousHash = block.Hash;
            }

            var anchors = await context.Kotwice
                .AsNoTracking()
                .Where(k => k.IdWybory == electionId)
                .OrderBy(k => k.Data)
                .Select(k => new ChainExportAnchor(k.Data, k.LiczbaBlokow, k.HashGlowy, k.Powod, k.Podpis))
                .ToListAsync();

            return new ChainExport(
                "internet-voting-chain/v2",
                Now(),
                new ChainExportElection(election.Id, election.Opis, election.DataRozpoczecia, election.DataZakonczenia, election.LiczbaBlokow, election.HashGlowy),
                signer.KeyId,
                signer.PublicKeyPem,
                candidates,
                exportBlocks,
                anchors);
        }

        public async Task<IReadOnlyList<AdminElectionViewModel>> GetAdminOverviewAsync()
        {
            var now = Now();
            var rows = await context.DataWyborows
                .AsNoTracking()
                .OrderByDescending(e => e.DataRozpoczecia)
                .Select(e => new
                {
                    e.Id,
                    e.Opis,
                    e.DataRozpoczecia,
                    e.DataZakonczenia,
                    e.HashGlowy,
                    e.LiczbaBlokow,
                    CandidateCount = e.Kandydats.Count,
                    Participants = e.GlosUzytkownikas.Count,
                    AnchorCount = e.Kotwice.Count,
                    HasFinalAnchor = e.Kotwice.Any(k => k.Powod == ReasonElectionEnded),
                    LastVerification = e.Weryfikacje.OrderByDescending(w => w.Data).ThenByDescending(w => w.Id).Select(w => new ChainVerificationViewModel
                    {
                        Date = w.Data,
                        IsValid = w.Poprawny,
                        BlockCount = w.LiczbaBlokow,
                        HeadHash = w.HashGlowy,
                        Trigger = w.Wyzwalacz,
                        Details = w.Szczegoly,
                    }).FirstOrDefault(),
                })
                .ToListAsync();

            return rows.Select(e => new AdminElectionViewModel
            {
                Id = e.Id,
                Opis = e.Opis,
                DataRozpoczecia = e.DataRozpoczecia,
                DataZakonczenia = e.DataZakonczenia,
                Status = DataWyborow.GetStatus(e.DataRozpoczecia, e.DataZakonczenia, now),
                CandidateCount = e.CandidateCount,
                BlockCount = e.LiczbaBlokow,
                Participants = e.Participants,
                HeadHash = e.HashGlowy,
                LastVerification = e.LastVerification,
                AnchorCount = e.AnchorCount,
                HasFinalAnchor = e.HasFinalAnchor,
            }).ToList();
        }

        public async Task<IReadOnlyList<int>> GetActiveElectionIdsAsync()
        {
            var now = Now();
            var horizon = now.Subtract(_options.VerifyEndedElectionsFor);
            return await context.DataWyborows
                .AsNoTracking()
                .Where(e => e.DataRozpoczecia <= now && e.DataZakonczenia >= horizon)
                .OrderBy(e => e.Id)
                .Select(e => e.Id)
                .ToListAsync();
        }

        public Task<bool> HasFinalAnchorAsync(int electionId)
        {
            return context.Kotwice.AnyAsync(k => k.IdWybory == electionId && k.Powod == ReasonElectionEnded);
        }

        private static ChainAnchorViewModel ToViewModel(KotwicaLancucha anchor)
        {
            return new ChainAnchorViewModel
            {
                Date = anchor.Data,
                BlockCount = anchor.LiczbaBlokow,
                HeadHash = anchor.HashGlowy,
                Reason = anchor.Powod,
                Recipients = anchor.Odbiorcy,
                Signature = anchor.Podpis,
            };
        }

        private DateTime Now() => timeProvider.GetLocalNow().DateTime;
    }
}
