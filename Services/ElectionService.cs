using InternetVotingApplication.Blockchain;
using InternetVotingApplication.Configuration;
using InternetVotingApplication.ExtensionMethods;
using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using InternetVotingApplication.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Data;

namespace InternetVotingApplication.Services
{
    /// <summary>Voter-facing operations: listing elections and appending a vote block to the chain.</summary>
    public class ElectionService(
        InternetVotingContext context,
        IBlockSigner signer,
        IChainService chainService,
        IEmailSender emailSender,
        IOptions<ChainOptions> chainOptions,
        TimeProvider timeProvider,
        ILogger<ElectionService> logger) : IElectionService
    {
        private const int MaxAttempts = 3;

        public async Task<DataWyborowViewModel> GetElectionListAsync(int userId)
        {
            var now = Now();
            var elections = await context.DataWyborows
                .AsNoTracking()
                .OrderByDescending(e => e.DataRozpoczecia)
                .Select(e => new
                {
                    e.Id,
                    e.Opis,
                    e.DataRozpoczecia,
                    e.DataZakonczenia,
                    CandidateCount = e.Kandydats.Count,
                    HasVoted = e.GlosUzytkownikas.Any(g => g.IdUzytkownik == userId),
                })
                .ToListAsync();

            return new DataWyborowViewModel
            {
                Elections = elections.Select(e => new DataWyborowItemViewModel
                {
                    Id = e.Id,
                    Opis = e.Opis,
                    DataRozpoczecia = e.DataRozpoczecia,
                    DataZakonczenia = e.DataZakonczenia,
                    CandidateCount = e.CandidateCount,
                    HasVoted = e.HasVoted,
                    Status = DataWyborow.GetStatus(e.DataRozpoczecia, e.DataZakonczenia, now),
                }).ToList(),
            };
        }

        public async Task<ElectionStatus?> GetElectionStatusAsync(int electionId)
        {
            var election = await context.DataWyborows.AsNoTracking().SingleOrDefaultAsync(e => e.Id == electionId);
            return election?.GetStatus(Now());
        }

        public async Task<KandydatViewModel?> GetVotingPageAsync(int electionId)
        {
            var election = await context.DataWyborows.AsNoTracking().SingleOrDefaultAsync(e => e.Id == electionId);
            if (election == null)
            {
                return null;
            }

            var candidates = await context.Kandydats
                .AsNoTracking()
                .Where(k => k.IdWybory == electionId)
                .OrderBy(k => k.Nazwisko)
                .ThenBy(k => k.Imie)
                .Select(k => new KandydatItemViewModel { Id = k.Id, Imie = k.Imie, Nazwisko = k.Nazwisko })
                .ToListAsync();

            return new KandydatViewModel
            {
                ElectionId = election.Id,
                ElectionName = election.Opis,
                DataZakonczenia = election.DataZakonczenia,
                Candidates = candidates,
            };
        }

        public Task<bool> HasVotedAsync(int userId, int electionId)
        {
            return context.GlosUzytkownikas.AnyAsync(g => g.IdUzytkownik == userId && g.IdWybory == electionId);
        }

        public async Task<VoteOutcome> CastVoteAsync(int userId, int electionId, int candidateId)
        {
            for (int attempt = 1; attempt <= MaxAttempts; attempt++)
            {
                var (outcome, retry) = await TryCastVoteAsync(userId, electionId, candidateId);
                if (!retry)
                {
                    if (outcome.Status == VoteStatus.Success)
                    {
                        await PublishPeriodicAnchorIfDueAsync(electionId);
                    }

                    return outcome;
                }

                context.ChangeTracker.Clear();
                logger.LogInformation("Vote of user {UserId} in election {ElectionId} lost a concurrency race (attempt {Attempt})", userId, electionId, attempt);
            }

            return new VoteOutcome(VoteStatus.Conflict);
        }

        /// <summary>
        /// Appends one block. Only the election row and the last block are read: the stored head state is the
        /// integrity check on the hot path, the full chain verification runs in the background.
        /// </summary>
        private async Task<(VoteOutcome Outcome, bool Retry)> TryCastVoteAsync(int userId, int electionId, int candidateId)
        {
            var now = Now();

            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var election = await context.DataWyborows.SingleOrDefaultAsync(e => e.Id == electionId);
            if (election == null)
            {
                return (new VoteOutcome(VoteStatus.ElectionNotFound), false);
            }

            switch (election.GetStatus(now))
            {
                case ElectionStatus.Upcoming:
                    return (new VoteOutcome(VoteStatus.ElectionNotStarted, ElectionName: election.Opis), false);
                case ElectionStatus.Ended:
                    return (new VoteOutcome(VoteStatus.ElectionEnded, ElectionName: election.Opis), false);
                default:
                    break;
            }

            if (!await context.Kandydats.AnyAsync(k => k.Id == candidateId && k.IdWybory == electionId))
            {
                return (new VoteOutcome(VoteStatus.CandidateNotInElection, ElectionName: election.Opis), false);
            }

            if (await HasVotedAsync(userId, electionId))
            {
                return (new VoteOutcome(VoteStatus.AlreadyVoted, ElectionName: election.Opis), false);
            }

            var head = await context.GlosowanieWyborczes
                .AsNoTracking()
                .Where(g => g.IdWybory == electionId)
                .OrderByDescending(g => g.Indeks)
                .FirstOrDefaultAsync();

            if (!await HeadIsSoundAsync(election, head))
            {
                await transaction.RollbackAsync();
                context.ChangeTracker.Clear();
                await chainService.VerifyAndStoreAsync(electionId, "Vote");
                return (new VoteOutcome(VoteStatus.ChainCorrupted, ElectionName: election.Opis), false);
            }

            var block = new GlosowanieWyborcze
            {
                Indeks = election.LiczbaBlokow,
                IdKandydat = candidateId,
                IdWybory = electionId,
                IdPoprzednie = head?.Id,
                ZnacznikCzasu = now,
                Nonce = BlockHelper.NewNonce(),
                IdKlucza = signer.KeyId,
            };
            block.Hash = BlockHelper.ComputeHash(block, head?.Hash);
            block.Podpis = signer.Sign(block.Hash);

            election.HashGlowy = block.Hash;
            election.LiczbaBlokow++;
            election.Wersja++;

            context.GlosowanieWyborczes.Add(block);
            context.GlosUzytkownikas.Add(new GlosUzytkownika { IdUzytkownik = userId, IdWybory = electionId, DataOddania = now });

            var email = await context.Uzytkowniks.Where(u => u.Id == userId).Select(u => u.Email).SingleAsync();
            await emailSender.SendAsync(Email.VoteReceipt(email, election.Opis, block.Hash));

            try
            {
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another vote in the same election committed first; the election row version moved on.
                await transaction.RollbackAsync();
                return (new VoteOutcome(VoteStatus.Conflict), true);
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                context.ChangeTracker.Clear();
                if (await HasVotedAsync(userId, electionId))
                {
                    logger.LogWarning(ex, "Duplicate vote of user {UserId} in election {ElectionId} rejected by a unique constraint", userId, electionId);
                    return (new VoteOutcome(VoteStatus.AlreadyVoted, ElectionName: election.Opis), false);
                }

                logger.LogWarning(ex, "Vote in election {ElectionId} rejected by a unique constraint; retrying", electionId);
                return (new VoteOutcome(VoteStatus.Conflict), true);
            }

            logger.LogInformation("Vote recorded in election {ElectionId}, block {Index}", electionId, block.Indeks);
            return (new VoteOutcome(VoteStatus.Success, block.Hash, election.Opis), false);
        }

        /// <summary>Cheap integrity check: stored head matches the last block, and that block hashes and verifies.</summary>
        private async Task<bool> HeadIsSoundAsync(DataWyborow election, GlosowanieWyborcze? head)
        {
            if (!BlockChainHelper.HeadMatches(election, head))
            {
                logger.LogError("Head state of election {ElectionId} does not match the last block", election.Id);
                return false;
            }

            if (head == null)
            {
                return true;
            }

            string? previousHash = null;
            if (head.IdPoprzednie.HasValue)
            {
                previousHash = await context.GlosowanieWyborczes
                    .AsNoTracking()
                    .Where(g => g.Id == head.IdPoprzednie.Value)
                    .Select(g => g.Hash)
                    .SingleOrDefaultAsync();
            }

            var hashOk = string.Equals(head.Hash, BlockHelper.ComputeHash(head, previousHash), StringComparison.OrdinalIgnoreCase);
            var signatureOk = signer.Verify(head.Hash, head.Podpis);
            if (!hashOk || !signatureOk)
            {
                logger.LogError("Head block {BlockId} of election {ElectionId} is invalid (hash ok: {HashOk}, signature ok: {SignatureOk})", head.Id, election.Id, hashOk, signatureOk);
            }

            return hashOk && signatureOk;
        }

        private async Task PublishPeriodicAnchorIfDueAsync(int electionId)
        {
            var every = chainOptions.Value.AnchorEveryBlocks;
            if (every <= 0)
            {
                return;
            }

            var blocks = await context.DataWyborows.AsNoTracking().Where(e => e.Id == electionId).Select(e => e.LiczbaBlokow).SingleAsync();
            if (blocks % every == 0)
            {
                await chainService.PublishAnchorAsync(electionId, ChainService.ReasonPeriodic);
            }
        }

        private DateTime Now() => timeProvider.GetLocalNow().DateTime;
    }
}
