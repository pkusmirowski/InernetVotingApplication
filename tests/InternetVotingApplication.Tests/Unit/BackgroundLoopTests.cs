using InternetVotingApplication.Configuration;
using InternetVotingApplication.Services;
using InternetVotingApplication.Services.Mail;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace InternetVotingApplication.Tests.Unit;

/// <summary>The background workers survive a failing cycle and stop cleanly with the host.</summary>
public class BackgroundLoopTests
{
    private static IServiceScopeFactory FailingScopes()
    {
        var scopes = Substitute.For<IServiceScopeFactory>();
        scopes.CreateScope().Returns(_ => throw new InvalidOperationException("database down"));
        return scopes;
    }

    [Fact]
    public async Task Chain_verification_worker_logs_a_failed_cycle_and_keeps_running()
    {
        var scopes = FailingScopes();
        using var worker = new ChainVerificationWorker(scopes, Options.Create(new ChainOptions { VerificationInterval = TimeSpan.FromMilliseconds(20) }), TestData.Clock(), TestData.Logger<ChainVerificationWorker>());

        await worker.StartAsync(CancellationToken.None);
        await WaitForCallsAsync(scopes, 2);
        await worker.StopAsync(CancellationToken.None);

        Assert.True(worker.ExecuteTask!.IsCompleted);
    }

    [Fact]
    public async Task Email_dispatcher_logs_a_failed_cycle_and_keeps_running()
    {
        var scopes = FailingScopes();
        using var dispatcher = new EmailDispatcher(scopes, new FakeSmtpTransport(), Options.Create(new MailOptions { PollInterval = TimeSpan.FromMilliseconds(20) }), TestData.Logger<EmailDispatcher>());

        await dispatcher.StartAsync(CancellationToken.None);
        await WaitForCallsAsync(scopes, 2);
        await dispatcher.StopAsync(CancellationToken.None);

        Assert.True(dispatcher.ExecuteTask!.IsCompleted);
    }

    private static async Task WaitForCallsAsync(IServiceScopeFactory scopes, int calls)
    {
        for (int i = 0; i < 200 && scopes.ReceivedCalls().Count() < calls; i++)
        {
            await Task.Delay(10);
        }

        Assert.True(scopes.ReceivedCalls().Count() >= calls, "the worker did not run a second cycle after a failure");
    }
}
