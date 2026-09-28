using InternetVotingApplication.Controllers;
using InternetVotingApplication.Interfaces;
using InternetVotingApplication.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using static InternetVotingApplication.Tests.Controllers.ControllerTestHelper;

namespace InternetVotingApplication.Tests.Controllers;

public class SetupControllerTests
{
    private static SetupController Create(string environmentName, ISetupDiagnostics diagnostics)
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        return new SetupController(environment, diagnostics).Prepare();
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Testing")]
    public async Task Returns_404_outside_development(string environmentName)
    {
        var diagnostics = Substitute.For<ISetupDiagnostics>();

        Assert.IsType<NotFoundResult>(await Create(environmentName, diagnostics).Index(CancellationToken.None));
        await diagnostics.DidNotReceiveWithAnyArgs().CollectAsync(default);
    }

    [Fact]
    public async Task Renders_diagnostics_in_development()
    {
        var diagnostics = Substitute.For<ISetupDiagnostics>();
        var model = new SetupViewModel { EnvironmentName = "Development" };
        diagnostics.CollectAsync(Arg.Any<CancellationToken>()).Returns(model);

        var view = AssertView(await Create("Development", diagnostics).Index(CancellationToken.None));

        Assert.Same(model, view.Model);
    }
}
