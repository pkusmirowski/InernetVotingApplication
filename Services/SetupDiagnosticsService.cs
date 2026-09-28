using InternetVotingApplication.Blockchain;
using InternetVotingApplication.Configuration;
using InternetVotingApplication.Data;
using InternetVotingApplication.Interfaces;
using InternetVotingApplication.Models;
using InternetVotingApplication.Services.Mail;
using InternetVotingApplication.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace InternetVotingApplication.Services;

/// <summary>
/// Builds the development diagnostics page. Every database call is guarded separately so the page renders
/// even when the database is broken; that is exactly when it is needed most.
/// </summary>
public sealed class SetupDiagnosticsService(
    InternetVotingContext context,
    DatabaseInfo databaseInfo,
    IBlockSigner signer,
    ISmtpTransport transport,
    IOptions<DatabaseOptions> databaseOptions,
    IOptions<SmtpOptions> smtpOptions,
    IOptions<SigningOptions> signingOptions,
    IOptions<SeedingOptions> seedingOptions,
    IHostEnvironment environment) : ISetupDiagnostics
{
    public async Task<SetupViewModel> CollectAsync(CancellationToken cancellationToken = default)
    {
        var vm = new SetupViewModel
        {
            EnvironmentName = environment.EnvironmentName,
            RuntimeVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            ApplicationVersion = AppVersion.Value,
            ContentRoot = environment.ContentRootPath,
            Provider = databaseInfo.Provider,
            Reason = databaseInfo.Reason,
            FallbackExplanation = databaseInfo.Explanation,
        };

        await CollectDatabaseAsync(vm, cancellationToken);
        CollectMail(vm);
        CollectSigning(vm);
        var adminExists = await CollectDataAsync(vm, cancellationToken);
        AddNextSteps(vm, adminExists);
        return vm;
    }

    private async Task CollectDatabaseAsync(SetupViewModel vm, CancellationToken cancellationToken)
    {
        var providerLabel = databaseInfo.Provider == DatabaseProvider.Sqlite ? "SQLite (plik)" : "SQL Server";
        var reasonLabel = databaseInfo.Reason switch
        {
            DatabaseSelectionReason.Fallback => "tryb zapasowy: SQL Server nie odpowiedział",
            DatabaseSelectionReason.Probed => "skonfigurowany, sonda OK",
            _ => "skonfigurowany",
        };
        vm.Database.Add(new SetupItem("Silnik", $"{providerLabel} ({reasonLabel})",
            databaseInfo.IsFallback ? SetupState.Warning : SetupState.Ok,
            databaseInfo.IsFallback ? "Uruchom usługę SQL Server i zrestartuj aplikację, żeby wrócić na SQL Server." : null));
        vm.Database.Add(new SetupItem("Połączenie", databaseInfo.MaskedConnectionString, SetupState.Ok));

        if (databaseInfo.SqliteFilePath is { } path)
        {
            var exists = File.Exists(path);
            vm.Database.Add(new SetupItem("Plik SQLite", path, exists ? SetupState.Ok : SetupState.Warning,
                "Usuń ten plik, żeby zacząć od pustej bazy (np. po zmianie modelu danych)."));
        }

        try
        {
            var canConnect = await context.Database.CanConnectAsync(cancellationToken);
            vm.Database.Add(new SetupItem("Dostęp do bazy", canConnect ? "OK" : "brak połączenia", canConnect ? SetupState.Ok : SetupState.Error));
            if (!canConnect)
            {
                return;
            }

            var creator = context.GetService<IRelationalDatabaseCreator>();
            var hasTables = await creator.HasTablesAsync(cancellationToken);
            vm.Database.Add(new SetupItem("Schemat", hasTables ? "utworzony" : "brak tabel", hasTables ? SetupState.Ok : SetupState.Error,
                hasTables ? null : "Włącz Database:ApplyMigrationsOnStartup albo uruchom 'dotnet ef database update'."));

            if (databaseInfo.Provider == DatabaseProvider.SqlServer)
            {
                var applied = (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).Count();
                var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
                vm.Database.Add(new SetupItem("Migracje", $"zastosowane: {applied}, oczekujące: {pending.Count}",
                    pending.Count == 0 ? SetupState.Ok : SetupState.Warning,
                    pending.Count == 0 ? null : $"Oczekujące: {string.Join(", ", pending)}. Włącz Database:ApplyMigrationsOnStartup albo uruchom 'dotnet ef database update'."));
            }
            else
            {
                vm.Database.Add(new SetupItem("Migracje", "nie dotyczy (SQLite: schemat z EnsureCreated, bez migracji)", SetupState.Ok));
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            vm.Database.Add(new SetupItem("Dostęp do bazy", "błąd", SetupState.Error, ex.Message));
            vm.Errors.Add($"Baza danych: {ex.Message}");
        }
    }

    private void CollectMail(SetupViewModel vm)
    {
        var smtp = smtpOptions.Value;
        if (!smtp.Enabled)
        {
            vm.Mail.Add(new SetupItem("Tryb", "wyłączona (Smtp:Enabled=false): wiadomości są tylko logowane", SetupState.Warning,
                "Linki aktywacyjne znajdziesz w logu aplikacji."));
            return;
        }

        if (transport is PickupDirectoryTransport pickup)
        {
            var count = Directory.Exists(pickup.Directory) ? Directory.GetFiles(pickup.Directory, "*.html").Length : 0;
            vm.Mail.Add(new SetupItem("Tryb", "pliki HTML w katalogu (bez serwera poczty)", SetupState.Ok));
            vm.Mail.Add(new SetupItem("Katalog", pickup.Directory, SetupState.Ok, "Każdy e-mail (aktywacja, reset hasła, potwierdzenie głosu, kotwica) to jeden plik. Otwórz go w przeglądarce."));
            vm.Mail.Add(new SetupItem("Wiadomości", count.ToString(System.Globalization.CultureInfo.InvariantCulture), SetupState.Ok));
        }
        else
        {
            vm.Mail.Add(new SetupItem("Tryb", $"SMTP {smtp.Host}:{smtp.Port} ({smtp.SecureSocket})", SetupState.Ok,
                "Jeśli serwer poczty nie działa, ustaw Smtp:PickupDirectory, żeby zapisywać wiadomości do plików."));
        }
    }

    private void CollectSigning(SetupViewModel vm)
    {
        var signing = signingOptions.Value;
        vm.Signing.Add(new SetupItem("Identyfikator klucza", signer.KeyId, SetupState.Ok));
        if (!string.IsNullOrWhiteSpace(signing.PrivateKeyPem))
        {
            vm.Signing.Add(new SetupItem("Źródło", "konfiguracja (Signing:PrivateKeyPem)", SetupState.Ok));
            return;
        }

        var path = Path.IsPathRooted(signing.KeyFilePath) ? signing.KeyFilePath : Path.Combine(environment.ContentRootPath, signing.KeyFilePath);
        vm.Signing.Add(new SetupItem("Plik klucza", path, File.Exists(path) ? SetupState.Ok : SetupState.Warning,
            "Zrób kopię tego pliku: bloki podpisane tym kluczem nie zweryfikują się bez niego."));
        if (signing.AutoGenerateKey)
        {
            vm.Signing.Add(new SetupItem("Generowanie", "automatyczne (Signing:AutoGenerateKey=true, tylko rozwój)", SetupState.Ok));
        }
    }

    private async Task<bool?> CollectDataAsync(SetupViewModel vm, CancellationToken cancellationToken)
    {
        var seeding = seedingOptions.Value;
        try
        {
            var elections = await context.DataWyborows.CountAsync(cancellationToken);
            var candidates = await context.Kandydats.CountAsync(cancellationToken);
            var users = await context.Uzytkowniks.CountAsync(cancellationToken);
            var activeUsers = await context.Uzytkowniks.CountAsync(u => u.JestAktywne, cancellationToken);
            var admins = await context.Administrators.CountAsync(cancellationToken);
            var blocks = await context.GlosowanieWyborczes.CountAsync(cancellationToken);
            var mails = await context.WiadomosciEmail.CountAsync(m => m.Wyslano == null, cancellationToken);

            vm.Data.Add(new SetupItem("Wybory / kandydaci", $"{elections} / {candidates}", elections > 0 ? SetupState.Ok : SetupState.Warning,
                elections > 0 ? null : (seeding.SampleData ? "Dane przykładowe pojawią się po restarcie." : "Włącz Seeding:SampleData albo dodaj wybory w panelu administratora.")));
            vm.Data.Add(new SetupItem("Konta (aktywne)", $"{users} ({activeUsers})", SetupState.Ok));
            vm.Data.Add(new SetupItem("Administratorzy", admins.ToString(System.Globalization.CultureInfo.InvariantCulture), admins > 0 ? SetupState.Ok : SetupState.Warning,
                admins > 0 ? null : (seeding.FirstActivatedUserIsAdmin ? "Pierwsze aktywowane konto zostanie administratorem." : "Wpisz adres konta w Seeding:AdminEmails.")));
            vm.Data.Add(new SetupItem("Bloki głosów", blocks.ToString(System.Globalization.CultureInfo.InvariantCulture), SetupState.Ok));
            vm.Data.Add(new SetupItem("E-maile w kolejce", mails.ToString(System.Globalization.CultureInfo.InvariantCulture), SetupState.Ok));
            return admins > 0;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            vm.Data.Add(new SetupItem("Dane", "niedostępne", SetupState.Error, ex.Message));
            vm.Errors.Add($"Dane: {ex.Message}");
            return null;
        }
    }

    private void AddNextSteps(SetupViewModel vm, bool? adminExists)
    {
        if (databaseInfo.IsFallback)
        {
            vm.NextSteps.Add("Uruchom usługę SQL Server (services.msc) i zrestartuj aplikację, jeśli chcesz pracować na SQL Serverze. Do klikania po aplikacji tryb zapasowy wystarcza.");
        }

        if (adminExists == false)
        {
            vm.NextSteps.Add("Zarejestruj konto (menu Rejestracja), potem otwórz najnowszy plik z katalogu poczty i kliknij link aktywacyjny.");
            if (seedingOptions.Value.FirstActivatedUserIsAdmin)
            {
                vm.NextSteps.Add("Pierwsze aktywowane konto dostaje rolę administratora: zaloguj się i wejdź w Panel administratora.");
            }

            vm.NextSteps.Add("Załóż drugie konto jako wyborcę (inny e-mail i inny poprawny PESEL, np. 02070803628) i oddaj głos w trwających wyborach.");
        }
        else if (adminExists == true)
        {
            vm.NextSteps.Add("Zaloguj się. Administrator zarządza wyborami w Panelu administratora; wyborca głosuje w Panelu głosowania.");
        }

        vm.NextSteps.Add("Po oddaniu głosu sprawdź hash w wyszukiwarce (Sprawdź głos) i obejrzyj stronę łańcucha wyborów z kluczem publicznym i kotwicami.");
        vm.NextSteps.Add("Stan usługi: /health. Ta strona (/setup) działa tylko w środowisku Development.");
        _ = databaseOptions;
    }
}
