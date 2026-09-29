using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using InternetVotingApplication.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace InternetVotingApplication.Services;

public class AdminService(InternetVotingContext context, IAuditLog auditLog, TimeProvider timeProvider, ILogger<AdminService> logger) : IAdminService
{
    public async Task<AddElectionStatus> AddElectionAsync(ElectionFormViewModel model, int? actorUserId = null)
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
        await auditLog.LogAsync(AuditLog.Actions.ElectionCreated, $"{name} ({model.DataRozpoczecia:g} - {model.DataZakonczenia:g})", actorUserId);
        return AddElectionStatus.Success;
    }

    public async Task<AddCandidateStatus> AddCandidateAsync(CandidateFormViewModel model, int? actorUserId = null)
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

        await auditLog.LogAsync(AuditLog.Actions.CandidateAdded, $"{firstName} {lastName}, wybory {model.IdWybory}", actorUserId);
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

    public async Task<IReadOnlyList<AuditEntryViewModel>> GetAuditLogAsync(int take)
    {
        return await context.DziennikAudytu
            .AsNoTracking()
            .OrderByDescending(a => a.Data)
            .ThenByDescending(a => a.Id)
            .Take(take)
            .Select(a => new AuditEntryViewModel { Date = a.Data, UserId = a.IdUzytkownik, Action = a.Akcja, Details = a.Szczegoly })
            .ToListAsync();
    }

    public async Task<DeleteCandidateStatus> DeleteCandidateAsync(int candidateId, int? actorUserId = null)
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
        await auditLog.LogAsync(AuditLog.Actions.CandidateDeleted, $"{candidate.Imie} {candidate.Nazwisko} (id {candidateId}), wybory {candidate.IdWybory}", actorUserId);
        return DeleteCandidateStatus.Success;
    }

    public Task<ElectionFormViewModel?> GetElectionAsync(int electionId)
    {
        return context.DataWyborows
            .AsNoTracking()
            .Where(e => e.Id == electionId)
            .Select(e => new ElectionFormViewModel { Opis = e.Opis, DataRozpoczecia = e.DataRozpoczecia, DataZakonczenia = e.DataZakonczenia })
            .SingleOrDefaultAsync();
    }

    public async Task<UpdateElectionStatus> UpdateElectionAsync(int electionId, ElectionFormViewModel model, int? actorUserId = null)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (model.DataRozpoczecia is null || model.DataZakonczenia is null || model.DataZakonczenia <= model.DataRozpoczecia)
        {
            return UpdateElectionStatus.InvalidDates;
        }

        var election = await context.DataWyborows.FindAsync(electionId);
        if (election == null)
        {
            return UpdateElectionStatus.NotFound;
        }

        var name = model.Opis.Trim();
        if (await context.DataWyborows.AnyAsync(e => e.Id != electionId && e.Opis == name))
        {
            return UpdateElectionStatus.Duplicate;
        }

        var before = $"{election.Opis} ({election.DataRozpoczecia:g} - {election.DataZakonczenia:g})";
        election.Opis = name;
        election.DataRozpoczecia = model.DataRozpoczecia.Value;
        election.DataZakonczenia = model.DataZakonczenia.Value;
        election.Wersja++;

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Election {ElectionId} update rejected by a unique constraint", electionId);
            return UpdateElectionStatus.Duplicate;
        }

        logger.LogInformation("Election {ElectionId} updated", electionId);
        await auditLog.LogAsync(AuditLog.Actions.ElectionUpdated, $"{before} -> {name} ({model.DataRozpoczecia:g} - {model.DataZakonczenia:g})", actorUserId);
        return UpdateElectionStatus.Success;
    }

    public async Task<DeleteElectionStatus> DeleteElectionAsync(int electionId, int? actorUserId = null)
    {
        var election = await context.DataWyborows.FindAsync(electionId);
        if (election == null)
        {
            return DeleteElectionStatus.NotFound;
        }

        if (await context.GlosowanieWyborczes.AnyAsync(g => g.IdWybory == electionId) || await context.GlosUzytkownikas.AnyAsync(g => g.IdWybory == electionId))
        {
            return DeleteElectionStatus.HasVotes;
        }

        // Every dependent table restricts deletes, so the rows go explicitly, in one SaveChanges with the election.
        context.Kandydats.RemoveRange(context.Kandydats.Where(k => k.IdWybory == electionId));
        context.Kotwice.RemoveRange(context.Kotwice.Where(k => k.IdWybory == electionId));
        context.Weryfikacje.RemoveRange(context.Weryfikacje.Where(w => w.IdWybory == electionId));
        context.DataWyborows.Remove(election);
        await context.SaveChangesAsync();

        logger.LogInformation("Election {ElectionId} deleted", electionId);
        await auditLog.LogAsync(AuditLog.Actions.ElectionDeleted, $"{election.Opis} (id {electionId})", actorUserId);
        return DeleteElectionStatus.Success;
    }

    public async Task<IReadOnlyList<UserListItemViewModel>> GetUsersAsync()
    {
        var now = timeProvider.GetLocalNow().DateTime;
        return await context.Uzytkowniks
            .AsNoTracking()
            .OrderBy(u => u.Id)
            .Select(u => new UserListItemViewModel
            {
                Id = u.Id,
                Imie = u.Imie,
                Nazwisko = u.Nazwisko,
                Email = u.Email,
                DataRejestracji = u.DataRejestracji,
                IsActive = u.JestAktywne,
                IsAdmin = u.Administrators.Any(),
                IsLockedOut = u.ZablokowaneDo != null && u.ZablokowaneDo > now,
                VoteCount = u.GlosUzytkownikas.Count,
            })
            .ToListAsync();
    }

    public async Task<UserActionStatus> ActivateUserAsync(int userId, int? actorUserId = null)
    {
        var user = await context.Uzytkowniks.FindAsync(userId);
        if (user == null)
        {
            return UserActionStatus.NotFound;
        }

        if (user.JestAktywne)
        {
            return UserActionStatus.NoChange;
        }

        user.JestAktywne = true;
        user.KodAktywacyjny = null;
        await context.SaveChangesAsync();
        logger.LogInformation("User {UserId} activated by an administrator", userId);
        await auditLog.LogAsync(AuditLog.Actions.UserActivated, $"{user.Email} (id {userId})", actorUserId);
        return UserActionStatus.Success;
    }

    public async Task<UserActionStatus> SetAdministratorAsync(int userId, bool isAdmin, int? actorUserId = null)
    {
        var user = await context.Uzytkowniks.FindAsync(userId);
        if (user == null)
        {
            return UserActionStatus.NotFound;
        }

        var existing = await context.Administrators.SingleOrDefaultAsync(a => a.IdUzytkownik == userId);
        if (isAdmin == (existing != null))
        {
            return UserActionStatus.NoChange;
        }

        if (existing == null)
        {
            context.Administrators.Add(new Administrator { IdUzytkownik = userId });
        }
        else
        {
            if (userId == actorUserId || await context.Administrators.CountAsync() <= 1)
            {
                return UserActionStatus.Forbidden;
            }

            context.Administrators.Remove(existing);
        }

        await context.SaveChangesAsync();
        logger.LogInformation("User {UserId} administrator role set to {IsAdmin}", userId, isAdmin);
        await auditLog.LogAsync(isAdmin ? AuditLog.Actions.AdminPromoted : AuditLog.Actions.AdminRevoked, $"{user.Email} (id {userId}, panel administratora)", actorUserId);
        return UserActionStatus.Success;
    }
}
