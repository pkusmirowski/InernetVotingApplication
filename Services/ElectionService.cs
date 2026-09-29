using System.Data;
using System.Data.Common;
using InternetVotingApplication.Blockchain;
using InternetVotingApplication.Configuration;
using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using InternetVotingApplication.Services.Mail;
using InternetVotingApplication.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace InternetVotingApplication.Services;

/// <summary>Voter-facing operations: listing elections and appending a vote block to the chain.</summary>
public class ElectionService(
    InternetVotingContext context,
    IBlockSigner signer,
    IChainService chainService,
    IEmailSender emailSender,
    IOptions<ChainOptions> chainOptions,
    TimeProvider timeProvider,
    ILogger<ElectionService> logger,
    IOptions<AppOptions>? appOptions = null) : IElectionService
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

        if (await CheckVotingRulesAsync(election, userId, candidateId, now) is { } refused)
        {
            return (new VoteOutcome(refused, ElectionName: election.Opis), false);
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
            await chainService.VerifyAndStoreAsync(electionId, ChainService.TriggerVote);
            return (new VoteOutcome(VoteStatus.ChainCorrupted, ElectionName: election.Opis), false);
        }

        var block = AppendBlock(election, head, candidateId, now);

        // Only the day is stored: an exact time equal to the block timestamp would let anyone reading the
        // database join a voter to their block.
        context.GlosUzytkownikas.Add(new GlosUzytkownika { IdUzytkownik = userId, IdWybory = electionId, DataOddania = now.Date });

        try
        {
            // The outbox sender saves the context, so the block, the participation row and the receipt are
            // written by this call; it must sit inside the try for a lost race to be retried.
            var email = await context.Uzytkowniks.Where(u => u.Id == userId).Select(u => u.Email).SingleAsync();
            await emailSender.SendAsync(Email.VoteReceipt(email, election.Opis, block.Hash, SearchLink(block.Hash)));
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Another vote in the same election committed first; the election row version moved on.
            await RollbackQuietlyAsync(transaction);
            return (new VoteOutcome(VoteStatus.Conflict), true);
        }
        catch (DbUpdateException ex)
        {
            await RollbackQuietlyAsync(transaction);
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

    /// <summary>The rules of the original application: the election is open, the candidate stands in it, one vote per voter.</summary>
    private async Task<VoteStatus?> CheckVotingRulesAsync(DataWyborow election, int userId, int candidateId, DateTime now)
    {
        switch (election.GetStatus(now))
        {
            case ElectionStatus.Upcoming:
                return VoteStatus.ElectionNotStarted;
            case ElectionStatus.Ended:
                return VoteStatus.ElectionEnded;
            default:
                break;
        }

        if (!await context.Kandydats.AnyAsync(k => k.Id == candidateId && k.IdWybory == election.Id))
        {
            return VoteStatus.CandidateNotInElection;
        }

        return await HasVotedAsync(userId, election.Id) ? VoteStatus.AlreadyVoted : null;
    }

    /// <summary>Creates the next block after <paramref name="head"/>, signs it and moves the election's head state to it.</summary>
    private GlosowanieWyborcze AppendBlock(DataWyborow election, GlosowanieWyborcze? head, int candidateId, DateTime now)
    {
        var block = new GlosowanieWyborcze
        {
            Indeks = election.LiczbaBlokow,
            IdKandydat = candidateId,
            IdWybory = election.Id,
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
        return block;
    }

    /// <summary>A deadlock victim's transaction is already rolled back by the server; a second rollback must not hide the retry.</summary>
    private async Task RollbackQuietlyAsync(IDbContextTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync();
        }
        catch (Exception ex) when (ex is InvalidOperationException or DbException)
        {
            logger.LogDebug(ex, "Rollback after a failed vote save was not needed");
        }
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

    /// <summary>Link to the public vote search, built only from the configured public address (never from the request).</summary>
    private string? SearchLink(string hash)
    {
        var baseUrl = appOptions?.Value.PublicBaseUrl;
        return string.IsNullOrWhiteSpace(baseUrl) ? null : $"{baseUrl.TrimEnd('/')}/Account/Search?hash={hash}";
    }

    private DateTime Now() => timeProvider.GetLocalNow().DateTime;
}
