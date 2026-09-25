# InternetVotingApplication

Aplikacja webowa do głosowań internetowych (praca inżynierska). Każdy oddany głos jest blokiem
w łańcuchu hashy SHA-256 prowadzonym osobno dla każdych wyborów. Wyborca otrzymuje hash swojego głosu
i może w każdej chwili sprawdzić, czy głos znajduje się w nienaruszonym łańcuchu.

Technologie: ASP.NET Core 9 MVC, Entity Framework Core 9, SQL Server, BCrypt, MailKit, Bootstrap 5, xUnit.

## Funkcje

- Rejestracja z walidacją numeru PESEL (suma kontrolna) i wieku, aktywacja konta linkiem e-mail.
- Logowanie cookie (ASP.NET Core Authentication) z rolami `Voter` i `Admin`, blokada konta po
  zbyt wielu nieudanych próbach, reset hasła jednorazowym linkiem, zmiana hasła.
- Panel wyborcy: lista wyborów ze statusem, głosowanie (jeden głos na wybory, weryfikowane w transakcji),
  potwierdzenie z hashem, wyniki po zakończeniu wyborów.
- Publiczna wyszukiwarka głosu po hashu z weryfikacją integralności całego łańcucha.
- Panel administratora: tworzenie wyborów, dodawanie i usuwanie kandydatów (bez oddanych głosów).
- Wysyłka e-maili w tle (kolejka + `BackgroundService`), więc awaria SMTP nie przerywa żądania.

## Struktura projektu

| Katalog | Zawartość |
| --- | --- |
| `Controllers/` | `Account`, `Election`, `Admin`, `Home` |
| `Services/` | logika domenowa (`UserService`, `ElectionService`, `AdminService`) i wysyłka poczty (`Services/Mail`) |
| `Blockchain/` | serializacja bloku, hashowanie, weryfikacja łańcucha |
| `Models/` | encje EF Core, `DbContext`, enumy statusów, modele formularzy logowania i haseł |
| `ViewModels/` | modele widoków i formularzy |
| `Data/` | inicjalizacja bazy (migracje, awans administratorów) |
| `Configuration/` | klasy opcji (`Smtp`, `Security`, `Seeding`, `Database`) |
| `Migrations/` | migracje EF Core |
| `tests/InternetVotingApplication.Tests/` | testy jednostkowe, serwisów (SQLite in-memory) i integracyjne (`WebApplicationFactory`) |
| `docs/` | analiza kodu i plan rozwoju |

## Uruchomienie lokalne

Wymagania: .NET SDK 9.0, SQL Server (lub Docker).

1. Baza danych i serwer SMTP do testów przez Docker:

   ```bash
   docker compose up -d db mailpit
   ```

   SQL Server nasłuchuje na `localhost:1433` (użytkownik `sa`, hasło `Voting!Passw0rd`),
   a Mailpit odbiera pocztę na porcie 1025 i pokazuje ją pod <http://localhost:8025>.
   `appsettings.Development.json` jest już skonfigurowany pod te usługi.

2. Uruchomienie aplikacji (w środowisku `Development` migracje są stosowane automatycznie):

   ```bash
   dotnet run --project InternetVotingApplication.csproj
   ```

   Alternatywnie ręcznie: `dotnet ef database update`.

3. Konto administratora: zarejestruj konto i aktywuj je linkiem z e-maila, a następnie wpisz jego adres
   w konfiguracji `Seeding:AdminEmails` (np. w user secrets) i zrestartuj aplikację. Przy starcie konto
   zostanie awansowane do roli administratora.

   ```bash
   dotnet user-secrets set "Seeding:AdminEmails:0" "admin@example.com"
   ```

Cała aplikacja w kontenerach (SQL Server + Mailpit + aplikacja na <http://localhost:8080>):

```bash
docker compose up --build
```

## Konfiguracja

Wartości wrażliwe nie są przechowywane w repozytorium. Lokalnie użyj user secrets, w produkcji zmiennych
środowiskowych lub Azure Key Vault.

| Klucz | Znaczenie |
| --- | --- |
| `ConnectionStrings:InternetVotingDBConnection` | połączenie z SQL Server |
| `Database:ApplyMigrationsOnStartup` | stosowanie migracji przy starcie (`true` w Development) |
| `Smtp:Host`, `Smtp:Port`, `Smtp:SecureSocket`, `Smtp:UserName`, `Smtp:Password`, `Smtp:FromAddress` | serwer poczty; `Smtp:Enabled=false` tylko loguje wiadomości |
| `Security:MaxFailedLoginAttempts`, `Security:LockoutDuration`, `Security:PasswordResetTokenLifetime` | polityka blokady konta i ważność linku resetu |
| `Seeding:AdminEmails` | adresy kont awansowanych do roli administratora przy starcie |

Przykład ustawienia sekretów SMTP:

```bash
dotnet user-secrets set "Smtp:UserName" "login"
dotnet user-secrets set "Smtp:Password" "haslo"
```

## Testy

```bash
dotnet test InternetVotingApplication.sln
```

Testy nie wymagają SQL Server ani SMTP: serwisy i testy integracyjne działają na SQLite in-memory,
a poczta jest przechwytywana przez `FakeEmailSender`.

## Łańcuch głosów

Blok (`GlosowanieWyborcze`) zawiera: indeks w łańcuchu, identyfikator kandydata i wyborów, znacznik czasu,
losowy 128-bitowy nonce, identyfikator i hash poprzedniego bloku oraz własny hash:

```text
hash = SHA256("v2|indeks|kandydat|wybory|czas|nonce|hashPoprzedniego")
```

Weryfikacja (`BlockChainHelper.VerifyBlockChain`) sprawdza w czasie O(n) ciągłość indeksów, powiązanie
z poprzednim blokiem i zgodność hashy. Uruchamiana jest przed każdym oddaniem głosu, na stronie wyników
i w wyszukiwarce głosu. Blok nie zawiera żadnej informacji o wyborcy; udział w głosowaniu jest zapisany
osobno w `GlosUzytkownika`.

Ograniczenia obecnego rozwiązania i kierunki rozwoju (podpisy bloków, drzewo Merkle, tajność głosu)
są opisane w `docs/ANALIZA_I_PLAN_ROZWOJU.md`.

## Licencja

MIT, patrz `LICENSE`.
