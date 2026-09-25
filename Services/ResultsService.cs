using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using InternetVotingApplication.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace InternetVotingApplication.Services
{
    /// <summary>Read-side: election results and public vote lookup. Uses the stored verification log.</summary>
    public class ResultsService(InternetVotingContext context, IChainService chainService, TimeProvider timeProvider) : IResultsService
    {
        public async Task<GlosowanieWyborczeViewModel?> GetResultsAsync(int electionId)
        {
            var election = await context.DataWyborows.AsNoTracking().SingleOrDefaultAsync(e => e.Id == electionId);
            if (election == null)
            {
                return null;
            }

            var rows = await context.Kandydats
                .AsNoTracking()
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

            return new GlosowanieWyborczeViewModel
            {
                ElectionId = election.Id,
                ElectionName = election.Opis,
                DataRozpoczecia = election.DataRozpoczecia,
                DataZakonczenia = election.DataZakonczenia,
                Status = election.GetStatus(timeProvider.GetLocalNow().DateTime),
                TotalVotes = total,
                Rows = rows.OrderByDescending(r => r.CountedVotes).ThenBy(r => r.CandidateSurname).ToList(),
                LastVerification = await EnsureVerificationAsync(electionId),
                BlockCount = election.LiczbaBlokow,
                HeadHash = election.HashGlowy,
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
                    g.Podpis,
                    g.IdKandydatNavigation.Imie,
                    g.IdKandydatNavigation.Nazwisko,
                    ElectionName = g.IdWyboryNavigation.Opis,
                })
                .SingleOrDefaultAsync();

            if (block == null)
            {
                return result;
            }

            var verification = await EnsureVerificationAsync(block.IdWybory);
            result.Found = true;
            result.ElectionId = block.IdWybory;
            result.CandidateName = block.Imie;
            result.CandidateSurname = block.Nazwisko;
            result.ElectionName = block.ElectionName;
            result.BlockIndex = block.Indeks;
            result.Timestamp = block.ZnacznikCzasu;
            result.Signature = block.Podpis;
            result.ChainValid = verification?.IsValid ?? false;
            result.VerifiedAt = verification?.Date;
            return result;
        }

        /// <summary>Returns the stored verification; runs one when the chain has never been verified.</summary>
        private async Task<ChainVerificationViewModel?> EnsureVerificationAsync(int electionId)
        {
            var last = await chainService.GetLastVerificationAsync(electionId);
            if (last != null)
            {
                return last;
            }

            await chainService.VerifyAndStoreAsync(electionId, "Results");
            return await chainService.GetLastVerificationAsync(electionId);
        }
    }
}
