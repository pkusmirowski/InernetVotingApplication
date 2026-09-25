using InternetVotingApplication.Blockchain;
using InternetVotingApplication.ExtensionMethods;
using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using InternetVotingApplication.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace InternetVotingApplication.Services
{
    public class ElectionService(
        InternetVotingContext context,
        IEmailSender emailSender,
        TimeProvider timeProvider,
        ILogger<ElectionService> logger) : IElectionService
    {
        public async Task<DataWyborowViewModel> GetElectionListAsync(int userId)
        {
            var now = Now();
            var elections = await context.DataWyborows
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
            var now = Now();

            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            var election = await context.DataWyborows.SingleOrDefaultAsync(e => e.Id == electionId);
            if (election == null)
            {
                return new VoteOutcome(VoteStatus.ElectionNotFound);
            }

            switch (election.GetStatus(now))
            {
                case ElectionStatus.Upcoming:
                    return new VoteOutcome(VoteStatus.ElectionNotStarted, ElectionName: election.Opis);
                case ElectionStatus.Ended:
                    return new VoteOutcome(VoteStatus.ElectionEnded, ElectionName: election.Opis);
                default:
                    break;
            }

            if (!await context.Kandydats.AnyAsync(k => k.Id == candidateId && k.IdWybory == electionId))
            {
                return new VoteOutcome(VoteStatus.CandidateNotInElection, ElectionName: election.Opis);
            }

            if (await HasVotedAsync(userId, electionId))
            {
                return new VoteOutcome(VoteStatus.AlreadyVoted, ElectionName: election.Opis);
            }

            var chain = await LoadChainAsync(electionId);
            var verification = BlockChainHelper.VerifyBlockChain(chain);
            if (!verification.IsValid)
            {
                logger.LogError("Hash chain of election {ElectionId} is corrupted; invalid blocks: {Blocks}", electionId, string.Join(',', verification.InvalidBlockIds));
                return new VoteOutcome(VoteStatus.ChainCorrupted, ElectionName: election.Opis);
            }

            var head = chain.Count == 0 ? null : chain[^1];
            var block = new GlosowanieWyborcze
            {
                Indeks = chain.Count,
                IdKandydat = candidateId,
                IdWybory = electionId,
                IdPoprzednie = head?.Id,
                ZnacznikCzasu = now,
                Nonce = BlockHelper.NewNonce(),
            };
            block.Hash = BlockHelper.ComputeHash(block, head?.Hash);

            context.GlosowanieWyborczes.Add(block);
            context.GlosUzytkownikas.Add(new GlosUzytkownika { IdUzytkownik = userId, IdWybory = electionId, DataOddania = now });

            try
            {
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateException ex)
            {
                // A concurrent request of the same voter (unique index on user + election) or of another
                // voter (unique index on election + block index) won the race.
                logger.LogWarning(ex, "Vote of user {UserId} in election {ElectionId} rejected by a unique constraint", userId, electionId);
                await transaction.RollbackAsync();
                return new VoteOutcome(VoteStatus.AlreadyVoted, ElectionName: election.Opis);
            }

            var email = await context.Uzytkowniks.Where(u => u.Id == userId).Select(u => u.Email).SingleAsync();
            await emailSender.SendAsync(Email.VoteReceipt(email, election.Opis, block.Hash));
            logger.LogInformation("Vote recorded in election {ElectionId}, block {Index}", electionId, block.Indeks);

            return new VoteOutcome(VoteStatus.Success, block.Hash, election.Opis);
        }

        public async Task<GlosowanieWyborczeViewModel?> GetResultsAsync(int electionId)
        {
            var election = await context.DataWyborows.AsNoTracking().SingleOrDefaultAsync(e => e.Id == electionId);
            if (election == null)
            {
                return null;
            }

            var rows = await context.Kandydats
                .Where(k => k.IdWybory == electionId)
                .Select(k => new GlosowanieWyborczeItemViewModel
                {
                    IdKandydat = k.Id,
                    CandidateName = k.Imie,
                    CandidateSurname = k.Nazwisko,
                    CountedVotes = k.GlosowanieWyborczes.Count,
                })
                .ToListAsync();

            var total = rows.Sum(r => r.CountedVotes);
            foreach (var row in rows)
            {
                row.CountedVotesPercentage = total == 0 ? 0 : Math.Round(100.0 * row.CountedVotes / total, 2);
            }

            var verification = await VerifyChainAsync(electionId);

            return new GlosowanieWyborczeViewModel
            {
                ElectionId = election.Id,
                ElectionName = election.Opis,
                DataRozpoczecia = election.DataRozpoczecia,
                DataZakonczenia = election.DataZakonczenia,
                Status = election.GetStatus(Now()),
                TotalVotes = total,
                Rows = rows.OrderByDescending(r => r.CountedVotes).ThenBy(r => r.CandidateSurname).ToList(),
                ChainValid = verification.IsValid,
                BlockCount = verification.BlockCount,
                HeadHash = verification.HeadHash,
            };
        }

        public async Task<VoteSearchViewModel> SearchVoteAsync(string hash)
        {
            var normalized = (hash ?? string.Empty).Trim().ToUpperInvariant();
            var result = new VoteSearchViewModel { Hash = normalized, Searched = normalized.Length > 0 };
            if (normalized.Length != 64)
            {
                return result;
            }

            var block = await context.GlosowanieWyborczes
                .AsNoTracking()
                .Where(g => g.Hash == normalized)
                .Select(g => new
                {
                    g.IdWybory,
                    g.Indeks,
                    g.ZnacznikCzasu,
                    g.IdKandydatNavigation.Imie,
                    g.IdKandydatNavigation.Nazwisko,
                    ElectionName = g.IdWyboryNavigation.Opis,
                })
                .SingleOrDefaultAsync();

            if (block == null)
            {
                return result;
            }

            var verification = await VerifyChainAsync(block.IdWybory);
            result.Found = true;
            result.CandidateName = block.Imie;
            result.CandidateSurname = block.Nazwisko;
            result.ElectionName = block.ElectionName;
            result.BlockIndex = block.Indeks;
            result.Timestamp = block.ZnacznikCzasu;
            result.ChainValid = verification.IsValid;
            return result;
        }

        public async Task<ChainVerificationResult> VerifyChainAsync(int electionId)
        {
            var chain = await LoadChainAsync(electionId);
            return BlockChainHelper.VerifyBlockChain(chain);
        }

        private Task<List<GlosowanieWyborcze>> LoadChainAsync(int electionId)
        {
            return context.GlosowanieWyborczes
                .AsNoTracking()
                .Where(g => g.IdWybory == electionId)
                .OrderBy(g => g.Indeks)
                .ToListAsync();
        }

        private DateTime Now() => timeProvider.GetLocalNow().DateTime;
    }
}
