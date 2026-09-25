using InternetVotingApplication.Configuration;
using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InternetVotingApplication.Services
{
    /// <summary>
    /// Periodically re-verifies every active chain and publishes the final anchor of elections that have ended.
    /// Keeps the O(n) verification out of the request path.
    /// </summary>
    public sealed class ChainVerificationWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<ChainOptions> options,
        TimeProvider timeProvider,
        ILogger<ChainVerificationWorker> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(options.Value.VerificationInterval);
            do
            {
                try
                {
                    await RunOnceAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Chain verification cycle failed");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        /// <summary>One verification cycle over all active elections. Public for tests.</summary>
        public async Task RunOnceAsync(CancellationToken cancellationToken)
        {
            using var scope = scopeFactory.CreateScope();
            var chain = scope.ServiceProvider.GetRequiredService<IChainService>();
            var context = scope.ServiceProvider.GetRequiredService<InternetVotingContext>();
            var now = timeProvider.GetLocalNow().DateTime;

            foreach (var electionId in await chain.GetActiveElectionIdsAsync())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await chain.VerifyAndStoreAsync(electionId, "Background");
                logger.LogInformation("Election {ElectionId}: {Blocks} blocks, valid={Valid}", electionId, result.BlockCount, result.IsValid);

                var end = await context.DataWyborows.AsNoTracking().Where(e => e.Id == electionId).Select(e => e.DataZakonczenia).SingleAsync(cancellationToken);
                if (end < now && !await chain.HasFinalAnchorAsync(electionId))
                {
                    await chain.PublishAnchorAsync(electionId, ChainService.ReasonElectionEnded);
                }
            }
        }
    }
}
