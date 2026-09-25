using InternetVotingApplication;
using InternetVotingApplication.Data;

var builder = WebApplication.CreateBuilder(args);

var startup = new Startup(builder.Configuration, builder.Environment);
startup.ConfigureServices(builder.Services);

var app = builder.Build();

startup.Configure(app);

await DbInitializer.InitializeAsync(app.Services, app.Configuration);

await app.RunAsync();

/// <summary>
/// Marker required by <c>WebApplicationFactory&lt;Program&gt;</c> in integration tests.
/// </summary>
public partial class Program
{
}
