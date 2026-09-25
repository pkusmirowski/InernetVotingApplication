using InternetVotingApplication;
using InternetVotingApplication.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

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
