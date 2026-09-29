<#
.SYNOPSIS
  Proste menu do włączania i wyłączania aplikacji bez Visual Studio (uruchamiane przez Aplikacja.cmd).
#>

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$healthUrl = 'http://localhost:5000/health'
$appUrl = 'https://localhost:5001/'

function Test-Running {
    try {
        (Invoke-WebRequest -Uri $healthUrl -UseBasicParsing -TimeoutSec 2).StatusCode -eq 200
    }
    catch {
        $false
    }
}

function Get-AppProcesses {
    Get-CimInstance Win32_Process |
        Where-Object {
            ($_.Name -eq 'InternetVotingApplication.exe') -or
            ($_.Name -eq 'dotnet.exe' -and $_.CommandLine -match 'InternetVotingApplication\.(csproj|dll)')
        }
}

function Start-App([switch]$Sqlite) {
    if (Test-Running) {
        Write-Host 'Aplikacja już działa.' -ForegroundColor Yellow
        return
    }

    $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', "`"$root\run.ps1`"")
    if ($Sqlite) {
        $arguments += '-Sqlite'
    }

    # The application runs in its own window, so this menu stays free for "Wyłącz".
    Start-Process powershell -ArgumentList $arguments -WorkingDirectory $root
    Write-Host 'Uruchamiam aplikację w osobnym oknie. Czekam, aż wystartuje (pierwszy start trwa dłużej)...'
    for ($i = 0; $i -lt 120; $i++) {
        if (Test-Running) {
            Write-Host 'Aplikacja działa. Otwieram przeglądarkę.' -ForegroundColor Green
            Start-Process $appUrl
            return
        }

        Start-Sleep -Seconds 1
    }

    Write-Host 'Aplikacja nie odpowiedziała w ciągu 2 minut. Sprawdź komunikaty w jej oknie.' -ForegroundColor Yellow
}

function Stop-App {
    $processes = @(Get-AppProcesses)
    if ($processes.Count -eq 0) {
        Write-Host 'Aplikacja nie jest uruchomiona.' -ForegroundColor Yellow
        return
    }

    $processes | ForEach-Object { Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue }
    Write-Host 'Aplikacja została wyłączona.' -ForegroundColor Green
}

function Show-Status {
    if (Test-Running) {
        Write-Host "Aplikacja działa: $appUrl" -ForegroundColor Green
    }
    else {
        Write-Host 'Aplikacja jest wyłączona.' -ForegroundColor Yellow
    }
}

while ($true) {
    Write-Host ''
    Write-Host '=== Wybory Prezydenckie ===' -ForegroundColor Cyan
    Show-Status
    Write-Host ''
    Write-Host '  1  Włącz (SQL Server)'
    Write-Host '  2  Włącz (SQLite, bez SQL Servera)'
    Write-Host '  3  Wyłącz'
    Write-Host '  4  Otwórz w przeglądarce'
    Write-Host '  0  Zamknij to okno'
    $choice = Read-Host 'Wybierz'

    switch ($choice) {
        '1' { Start-App }
        '2' { Start-App -Sqlite }
        '3' { Stop-App }
        '4' { Start-Process $appUrl }
        '0' { return }
        default { Write-Host 'Wpisz 1, 2, 3, 4 albo 0.' -ForegroundColor Yellow }
    }
}
