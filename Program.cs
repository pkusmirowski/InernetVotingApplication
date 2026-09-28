using InternetVotingApplication;
using InternetVotingApplication.Data;
using Serilog;

// Polish diagnostics must render correctly in the classic Windows console window.
Console.OutputEncoding = System.Text.Encoding.UTF8;

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

try
{
    await DbInitializer.InitializeAsync(app.Services, app.Configuration);
}
catch (DatabaseUnavailableException ex)
{
    app.Logger.LogCritical("Aplikacja nie wystartowała, bo baza danych jest niedostępna.{NewLine}{Explanation}", Environment.NewLine, ex.Message);
    app.Logger.LogDebug(ex, "Szczegóły techniczne błędu bazy danych");
    return 1;
}
catch (Exception ex) when (ex is not OperationCanceledException)
{
    app.Logger.LogCritical(ex, "Aplikacja nie wystartowała: inicjalizacja bazy danych nie powiodła się.");
    return 1;
}

await app.RunAsync();
return 0;

/// <summary>
/// Marker required by <c>WebApplicationFactory&lt;Program&gt;</c> in integration tests.
/// </summary>
public partial class Program
{
}
