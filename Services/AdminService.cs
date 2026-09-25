using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using InternetVotingApplication.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace InternetVotingApplication.Services
{
    public class AdminService(InternetVotingContext context, ILogger<AdminService> logger) : IAdminService
    {
        public async Task<AddElectionStatus> AddElectionAsync(ElectionFormViewModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            if (model.DataRozpoczecia is null || model.DataZakonczenia is null || model.DataZakonczenia <= model.DataRozpoczecia)
            {
                return AddElectionStatus.InvalidDates;
            }

            var name = model.Opis.Trim();
            if (await context.DataWyborows.AnyAsync(e => e.Opis == name))
            {
                return AddElectionStatus.Duplicate;
            }

            context.DataWyborows.Add(new DataWyborow
            {
                Opis = name,
                DataRozpoczecia = model.DataRozpoczecia.Value,
                DataZakonczenia = model.DataZakonczenia.Value,
            });

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogWarning(ex, "Election '{Name}' rejected by a unique constraint", name);
                return AddElectionStatus.Duplicate;
            }

            logger.LogInformation("Election '{Name}' created", name);
            return AddElectionStatus.Success;
        }

        public async Task<AddCandidateStatus> AddCandidateAsync(CandidateFormViewModel model)
        {
            ArgumentNullException.ThrowIfNull(model);

            if (model.IdWybory is null || !await context.DataWyborows.AnyAsync(e => e.Id == model.IdWybory))
            {
                return AddCandidateStatus.ElectionNotFound;
            }

            var firstName = model.Imie.Trim();
            var lastName = model.Nazwisko.Trim();
            var duplicate = await context.Kandydats.AnyAsync(k =>
                k.IdWybory == model.IdWybory && k.Imie == firstName && k.Nazwisko == lastName);
            if (duplicate)
            {
                return AddCandidateStatus.Duplicate;
            }

            context.Kandydats.Add(new Kandydat { Imie = firstName, Nazwisko = lastName, IdWybory = model.IdWybory.Value });

            try
            {
                await context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                logger.LogWarning(ex, "Candidate rejected by a unique constraint");
                return AddCandidateStatus.Duplicate;
            }

            return AddCandidateStatus.Success;
        }

        public async Task<IReadOnlyList<ElectionOptionViewModel>> GetElectionOptionsAsync()
        {
            return await context.DataWyborows
                .OrderByDescending(e => e.DataRozpoczecia)
                .Select(e => new ElectionOptionViewModel(e.Id, e.Opis))
                .ToListAsync();
        }

        public async Task<CandidateListViewModel> GetCandidatesAsync(int? electionId)
        {
            var query = context.Kandydats.AsQueryable();
            if (electionId.HasValue)
            {
                query = query.Where(k => k.IdWybory == electionId);
            }

            var candidates = await query
                .OrderBy(k => k.IdWyboryNavigation.DataRozpoczecia)
                .ThenBy(k => k.Nazwisko)
                .ThenBy(k => k.Imie)
                .Select(k => new CandidateListItemViewModel
                {
                    Id = k.Id,
                    Imie = k.Imie,
                    Nazwisko = k.Nazwisko,
                    ElectionId = k.IdWybory,
                    ElectionName = k.IdWyboryNavigation.Opis,
                    VoteCount = k.GlosowanieWyborczes.Count,
                })
                .ToListAsync();

            return new CandidateListViewModel
            {
                SelectedElectionId = electionId,
                Elections = await GetElectionOptionsAsync(),
                Candidates = candidates,
            };
        }

        public async Task<DeleteCandidateStatus> DeleteCandidateAsync(int candidateId)
        {
            var candidate = await context.Kandydats.FindAsync(candidateId);
            if (candidate == null)
            {
                return DeleteCandidateStatus.NotFound;
            }

            if (await context.GlosowanieWyborczes.AnyAsync(g => g.IdKandydat == candidateId))
            {
                return DeleteCandidateStatus.HasVotes;
            }

            context.Kandydats.Remove(candidate);
            await context.SaveChangesAsync();
            logger.LogInformation("Candidate {CandidateId} deleted", candidateId);
            return DeleteCandidateStatus.Success;
        }
    }
}
