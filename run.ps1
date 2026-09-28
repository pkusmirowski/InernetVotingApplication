<#
.SYNOPSIS
  Uruchamia aplikację do głosowania na tym komputerze i otwiera stronę diagnostyczną.

.DESCRIPTION
  1. Sprawdza, czy jest .NET SDK 10 (jeśli nie, pokazuje polecenie instalacji).
  2. Wykrywa lokalne instancje SQL Server i stan ich usług; dla instancji nazwanej proponuje
     connection string i zapisuje go w user secrets (tylko po potwierdzeniu).
  3. Startuje aplikację profilem "https (SQL Server)" albo "https (SQLite)" (-Sqlite).
  Aplikacja sama tworzy bazę, dane przykładowe i klucz podpisu; e-maile trafiają do App_Data\mail.

.PARAMETER Sqlite
  Uruchom na pliku SQLite bez SQL Servera.
#>
param(
    [switch]$Sqlite
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $root

function Write-Step($text) { Write-Host "==> $text" -ForegroundColor Cyan }

Write-Step 'Sprawdzam .NET SDK'
$sdks = @()
try { $sdks = & dotnet --list-sdks 2>$null } catch { }
if (-not ($sdks | Where-Object { $_ -match '^10\.' })) {
    Write-Host 'Brak .NET SDK 10. Zainstaluj go poleceniem:' -ForegroundColor Yellow
    Write-Host '    winget install Microsoft.DotNet.SDK.10' -ForegroundColor Yellow
    Write-Host 'albo ze strony https://dotnet.microsoft.com/download/dotnet/10.0 i uruchom skrypt ponownie.'
    exit 1
}
Write-Host ("Znaleziono: " + (($sdks | Where-Object { $_ -match '^10\.' }) -join ', '))

$profile = 'https (SQL Server)'
if ($Sqlite) {
    $profile = 'https (SQLite)'
    Write-Step 'Tryb SQLite wybrany parametrem -Sqlite'
}
else {
    Write-Step 'Szukam instancji SQL Server'
    $instances = @{}
    $key = 'HKLM:\SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL'
    if (Test-Path $key) {
        (Get-Item $key).Property | ForEach-Object { $instances[$_] = (Get-ItemProperty $key).$_ }
    }
    if ($instances.Count -eq 0) {
        Write-Host 'Nie znaleziono zainstalowanej instancji SQL Server.' -ForegroundColor Yellow
        Write-Host 'Aplikacja spróbuje połączyć się z localhost, a jeśli się nie uda, wystartuje na SQLite (tryb zapasowy).'
    }
    else {
        foreach ($name in $instances.Keys) {
            $serviceName = if ($name -eq 'MSSQLSERVER') { 'MSSQLSERVER' } else { "MSSQL`$$name" }
            $service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
            $state = if ($service) { $service.Status } else { 'brak usługi' }
            $server = if ($name -eq 'MSSQLSERVER') { 'localhost' } else { "localhost\$name" }
            Write-Host ("  {0,-14} usługa: {1,-10} Server={2}" -f $name, $state, $server)
            if ($service -and $service.Status -ne 'Running') {
                $answer = Read-Host "  Usługa $serviceName jest zatrzymana. Uruchomić ją teraz? (wymaga uprawnień administratora) [t/N]"
                if ($answer -match '^[tTyY]') {
                    try { Start-Service -Name $serviceName; Write-Host '  Usługa uruchomiona.' -ForegroundColor Green }
                    catch { Write-Host "  Nie udało się uruchomić usługi: $($_.Exception.Message)" -ForegroundColor Yellow }
                }
            }
        }
        if (-not $instances.ContainsKey('MSSQLSERVER')) {
            $named = ($instances.Keys | Select-Object -First 1)
            $connection = "Server=localhost\$named;Database=InternetVoting;Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True;"
            $answer = Read-Host "Brak domyślnej instancji. Zapisać w user secrets połączenie do localhost\$named? [t/N]"
            if ($answer -match '^[tTyY]') {
                & dotnet user-secrets set 'ConnectionStrings:InternetVotingDBConnection' $connection --project InternetVotingApplication.csproj | Out-Null
                Write-Host 'Zapisano.' -ForegroundColor Green
            }
        }
    }
}

Write-Step "Uruchamiam aplikację (profil: $profile). Zatrzymanie: Ctrl+C"
Write-Host 'Przeglądarka otworzy się na https://localhost:5001/Home/Setup (strona diagnostyczna).'
& dotnet run --project InternetVotingApplication.csproj --launch-profile $profile
exit $LASTEXITCODE
