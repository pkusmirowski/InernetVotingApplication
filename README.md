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
- Każdy blok podpisany ECDSA P-256 kluczem trzymanym poza bazą; klucz publiczny jest publikowany.
- Stan głowy łańcucha w wierszu wyborów: oddanie głosu czyta tylko głowę, pełna weryfikacja działa w tle
  i jest zapisywana w dzienniku weryfikacji.
- Kotwice: podpisane migawki głowy wysyłane e-mailem do komisji co N bloków i po zakończeniu wyborów.
- Publiczna wyszukiwarka głosu po hashu, publiczna strona łańcucha (`/Election/Chain/{id}`) i eksport JSON
  (`/Election/Export/{id}`) weryfikowalny niezależnym narzędziem `tools/ChainVerifier`.
- Panel administratora: wybory i łańcuchy (weryfikacja na żądanie, publikacja kotwicy), kandydaci,
  dziennik audytu.
- Poczta przez outbox (tabela + dispatcher z ponawianiem): wiadomość zapisuje się w tej samej transakcji
  co głos i nie ginie przy restarcie.
- Ograniczenie liczby żądań na logowaniu i rejestracji, `/health`, logi strukturalne (Serilog).

## Struktura projektu

| Katalog | Zawartość |
| --- | --- |
| `Controllers/` | `Account`, `Election`, `Admin`, `Home` |
| `Services/` | logika domenowa (`UserService`, `ElectionService`, `AdminService`) i wysyłka poczty (`Services/Mail`) |
| `Blockchain/` | serializacja bloku, hashowanie, podpis ECDSA, weryfikacja łańcucha i głowy |
| `Models/` | encje EF Core, `DbContext`, enumy statusów, modele formularzy logowania i haseł |
| `ViewModels/` | modele widoków i formularzy |
| `Data/` | inicjalizacja bazy (migracje, awans administratorów) |
| `Configuration/` | klasy opcji (`Smtp`, `Security`, `Seeding`, `Database`) |
| `Migrations/` | migracje EF Core |
| `tests/InternetVotingApplication.Tests/` | testy jednostkowe, serwisów (SQLite in-memory) i integracyjne (`WebApplicationFactory`) |
| `tools/ChainVerifier/` | niezależny weryfikator eksportu łańcucha (konsola, bez zależności od aplikacji) |
| `docs/` | analiza kodu i plan rozwoju |

## Uruchomienie lokalne (Windows, Visual Studio lub `dotnet run`)

Wymagania: .NET SDK 9.0 (<https://dotnet.microsoft.com/download>), SQL Server (Express, Developer lub LocalDB).
Nie potrzebujesz serwera poczty ani Dockera.

1. **Baza danych.** `appsettings.Development.json` łączy się z lokalną instancją przez uwierzytelnianie Windows:

   ```text
   Server=localhost;Database=InternetVoting;Trusted_Connection=True;TrustServerCertificate=True;
   ```

   Jeśli masz SQL Server Express albo LocalDB, zmień `Server=` na `localhost\SQLEXPRESS` lub
   `(localdb)\MSSQLLocalDB`. Najlepiej zrobić to w user secrets, żeby nie modyfikować pliku w repozytorium:

   ```bash
   dotnet user-secrets set "ConnectionStrings:InternetVotingDBConnection" "Server=localhost\SQLEXPRESS;Database=InternetVoting;Trusted_Connection=True;TrustServerCertificate=True;"
   ```

   Baza `InternetVoting` zostanie utworzona automatycznie przy pierwszym starcie (migracje EF Core).

2. **Start.** W Visual Studio otwórz `InternetVotingApplication.sln` i naciśnij F5 (profil `https`), albo w terminalu:

   ```bash
   dotnet run --project InternetVotingApplication.csproj
   ```

   Przy pierwszym starcie w trybie `Development` aplikacja:
   - tworzy schemat bazy i trzy przykładowe wybory z kandydatami (trwające, nadchodzące, zakończone),
   - generuje klucz podpisu bloków do `App_Data/signing-key.pem` (plik jest w `.gitignore`; zrób jego kopię),
   - zapisuje każdy e-mail jako plik HTML w `App_Data/mail/` zamiast go wysyłać.

3. **Pierwsze konto.** Zarejestruj się w aplikacji, otwórz plik z `App_Data/mail/` i kliknij link aktywacyjny.
   Pierwsze aktywowane konto automatycznie dostaje rolę administratora (`Seeding:FirstActivatedUserIsAdmin`).
   Kolejne konta są zwykłymi wyborcami. Administrator nie głosuje; do przetestowania głosowania załóż drugie konto
   (potrzebny drugi poprawny PESEL, np. `02070803628`).

4. **Co obejrzeć.** Panel wyborcy `/Election/Dashboard`, głosowanie i potwierdzenie z hashem, wyszukiwarka
   `/Account/Search`, publiczna strona łańcucha `/Election/Chain/{id}` z kluczem publicznym i kotwicami,
   panel administratora `/Admin/Elections` (weryfikacja, kotwica), dziennik `/Admin/Audit`, stan `/health`.
   Kotwice i potwierdzenia trafiają do `App_Data/mail/`.

Wariant z Dockerem (SQL Server + Mailpit na <http://localhost:8025> + aplikacja na <http://localhost:8080>):

```bash
docker compose up --build
```

Tylko baza i poczta w Dockerze, aplikacja lokalnie: `docker compose up -d db mailpit`, a w user secrets ustaw
połączenie `Server=localhost,1433;Database=InternetVoting;User Id=sa;Password=Voting!Passw0rd;TrustServerCertificate=True;`
oraz `Smtp:PickupDirectory` na pusty string, żeby poczta szła do Mailpit.

### Konto administratora w produkcji

Wpisz adres zarejestrowanego i aktywowanego konta w `Seeding:AdminEmails` i zrestartuj aplikację:

```bash
dotnet user-secrets set "Seeding:AdminEmails:0" "admin@example.com"
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
| `Seeding:AdminEmails`, `Seeding:FirstActivatedUserIsAdmin`, `Seeding:SampleData` | administratorzy i dane przykładowe (dwa ostatnie tylko do rozwoju) |
| `Smtp:PickupDirectory` | katalog na pliki HTML z pocztą zamiast wysyłki (rozwój) |
| `Signing:PrivateKeyPem`, `Signing:KeyFilePath`, `Signing:AutoGenerateKey` | klucz ECDSA do podpisu bloków i kotwic |
| `Chain:VerificationInterval`, `Chain:AnchorEveryBlocks`, `Chain:AnchorRecipients`, `Chain:VerifyEndedElectionsFor` | częstość weryfikacji w tle, kotwice i ich odbiorcy |
| `Mail:PollInterval`, `Mail:MaxAttempts`, `Mail:BatchSize` | dispatcher outboxa |
| `RateLimiting:AuthPermitLimit`, `RateLimiting:AuthWindow` | limit żądań na logowaniu, rejestracji i odzyskiwaniu hasła |

Przykład ustawienia sekretów SMTP:

```bash
dotnet user-secrets set "Smtp:UserName" "login"
dotnet user-secrets set "Smtp:Password" "haslo"
```

## Testy

```bash
dotnet test InternetVotingApplication.sln
dotnet test InternetVotingApplication.sln --settings tests/coverage.runsettings --collect:"XPlat Code Coverage"   # z pokryciem
```

Testy nie wymagają SQL Server ani SMTP: serwisy i testy integracyjne działają na SQLite in-memory,
a poczta jest przechwytywana przez `FakeEmailSender`. Poziomy testów:

| Katalog | Co sprawdza |
| --- | --- |
| `Unit/` | algorytmy i reguły: PESEL, wiek, SHA-256, podpis ECDSA, weryfikacja łańcucha, walidacja formularzy, szablony e-mail, nagłówki, konfiguracja, weryfikator offline |
| `Services/` | serwisy na prawdziwym EF Core (SQLite): rejestracja, logowanie, blokada, reset hasła, głosowanie, wyścigi, wyniki, kotwice, outbox, worker, seeding |
| `Controllers/` | kontrolery w izolacji (NSubstitute): przekierowania, komunikaty, `TempData`, mapowanie statusów |
| `Integration/` | cała aplikacja przez `WebApplicationFactory`: strony publiczne, autoryzacja, CSRF, rate limiting, pełny przebieg wyborcy i administratora |

## Styl kodu

Projekt używa aktualnych konwencji C# 13 / .NET 9: przestrzenie nazw w zapisie plikowym, konstruktory
podstawowe, rekordy, wyrażenia kolekcji, `TimeProvider`, `required`/nullable, `GeneratedRegex`.
Reguły są w `.editorconfig` na poziomie ostrzeżeń, `dotnet format` je egzekwuje, a CI odrzuca odstępstwa.

## Łańcuch głosów

Blok (`GlosowanieWyborcze`) zawiera: indeks w łańcuchu, identyfikator kandydata i wyborów, znacznik czasu,
losowy 128-bitowy nonce, identyfikator i hash poprzedniego bloku, własny hash oraz podpis:

```text
hash   = SHA256("v2|indeks|kandydat|wybory|czas|nonce|hashPoprzedniego")
podpis = ECDSA-P256-SHA256(klucz prywatny serwera, hash)      # base64, DER
```

Wiersz wyborów przechowuje **głowę łańcucha** (`hashGlowy`, `liczbaBlokow`) z tokenem współbieżności.
Oddanie głosu w transakcji `Serializable` czyta wiersz wyborów i ostatni blok, sprawdza zgodność głowy,
hash i podpis ostatniego bloku, dopisuje nowy blok i przesuwa głowę. Dwie równoległe próby w tych samych
wyborach rozstrzyga token współbieżności (przegrany ponawia do trzech razy).

**Pełna weryfikacja** (`ChainService.VerifyAndStoreAsync`) sprawdza w O(n) ciągłość indeksów, powiązania,
hashe, podpisy i zgodność głowy; wynik trafia do tabeli `WeryfikacjaLancucha`. Uruchamia ją worker w tle
(`Chain:VerificationInterval`), administrator na żądanie oraz strona wyników, gdy łańcuch nie był jeszcze
weryfikowany.

**Kotwice** (`KotwicaLancucha`) to podpisane migawki głowy: `anchor-v1|wybory|liczbaBlokow|hashGlowy|czas`.
Publikowane co `Chain:AnchorEveryBlocks` bloków, po zakończeniu wyborów i ręcznie; wysyłane e-mailem do
`Chain:AnchorRecipients`. Kopia poza systemem pozwala wykryć przepisanie lub skrócenie historii nawet przez
osobę mającą dostęp do bazy i klucza.

Blok nie zawiera żadnej informacji o wyborcy; udział w głosowaniu jest zapisany osobno w `GlosUzytkownika`.
Ograniczenia (korelacja czasowa w jednej bazie) i kierunki rozwoju są opisane w `docs/ARCHITEKTURA.md`.

## Weryfikacja niezależna od aplikacji

1. Pobierz eksport: `https://<host>/Election/Export/{id}` (lub przycisk na `/Election/Chain/{id}`).
2. Uruchom weryfikator:

   ```bash
   dotnet run --project tools/ChainVerifier -- election-1-chain.json
   ```

Narzędzie ma własną implementację reguł (SHA-256, ECDSA, format eksportu) i nie korzysta z kodu aplikacji.
Sprawdza indeksy, powiązania, hashe, podpisy, zgodność nagłówka, zgodność kotwic z łańcuchem i liczy głosy.
Zwraca kod 0 dla poprawnego łańcucha, 1 dla niepoprawnego.

## Eksploatacja

- `GET /health` sprawdza połączenie z bazą.
- Logi w formacie Serilog na konsolę, z logowaniem żądań HTTP; poziomy w sekcji `Serilog`.
- Dziennik audytu (`/Admin/Audit`) zapisuje działania administratorów, wyniki weryfikacji i kotwice.

## Licencja

MIT, patrz `LICENSE`.
