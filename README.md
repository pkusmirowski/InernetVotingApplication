# InternetVotingApplication

Aplikacja webowa do głosowań internetowych (praca inżynierska). Każdy oddany głos jest blokiem
w łańcuchu hashy SHA-256 prowadzonym osobno dla każdych wyborów. Wyborca otrzymuje hash swojego głosu
i może w każdej chwili sprawdzić, czy głos znajduje się w nienaruszonym łańcuchu.

Technologie: .NET 10 LTS, ASP.NET Core 10 MVC, Entity Framework Core 10, SQL Server, BCrypt, MailKit, Bootstrap 5, xUnit.
Wersja 2.0.0; historia zmian w `CHANGELOG.md`.

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

## Szybki start (Windows, Visual Studio)

Potrzebujesz tylko **.NET SDK 10.0** (<https://dotnet.microsoft.com/download/dotnet/10.0>, albo
`winget install Microsoft.DotNet.SDK.10`) i Visual Studio 2022 17.14+ lub Visual Studio 2026.
SQL Server jest opcjonalny: bez niego aplikacja w trybie Development uruchomi się na pliku SQLite i powie Ci o tym.

1. Otwórz `InternetVotingApplication.sln` w Visual Studio.
2. Wybierz profil startowy **`https (SQL Server)`** (domyślny) i naciśnij **F5**.
3. Przeglądarka otworzy stronę **Diagnostyka** (`/setup`). Pokazuje, na jakiej bazie działa aplikacja,
   gdzie trafiają e-maile, gdzie jest klucz podpisu i co zrobić dalej. Idź według listy „Co dalej".

Co dzieje się przy pierwszym starcie w trybie Development:

- aplikacja sprawdza (maksymalnie 3 s), czy SQL Server `localhost` odpowiada; jeśli tak, tworzy bazę
  `InternetVoting` migracjami EF Core; jeśli nie, przechodzi w **tryb zapasowy SQLite**
  (`App_Data/voting-dev.db`) i wyświetla żółty pasek na dole każdej strony oraz wyjaśnienie w logu,
- dodaje trzy przykładowe wybory z kandydatami (trwające, nadchodzące, zakończone),
- generuje klucz podpisu bloków do `App_Data/signing-key.pem` (zrób jego kopię),
- zapisuje każdy e-mail jako plik HTML w `App_Data/mail/` (aktywacja konta, reset hasła, potwierdzenie
  głosu, kotwice łańcucha) zamiast wysyłać go przez SMTP,
- pierwsze aktywowane konto dostaje rolę administratora; kolejne są zwykłymi wyborcami.

Profile startowe (`Properties/launchSettings.json`):

| Profil | Baza | Kiedy |
| --- | --- | --- |
| `https (SQL Server)` | SQL Server, a gdy nie odpowiada: SQLite (tylko Development) | domyślny, codzienna praca |
| `https (SQLite)` | zawsze plik SQLite, bez sondy SQL Servera | bez SQL Servera, szybkie klikanie |
| `http` | jak `https (SQL Server)`, bez TLS | problemy z certyfikatem deweloperskim |
| `IIS Express` | jak `https (SQL Server)` | jeśli wolisz IIS Express |

### Bez Visual Studio

Dwuklik w `run.cmd` (albo `run.cmd -Sqlite`). Skrypt sprawdza SDK, wykrywa instancje SQL Server i stan ich
usług, proponuje connection string dla instancji nazwanej, uruchamia aplikację i otwiera przeglądarkę.
Testy: `run-tests.cmd`. W terminalu: `dotnet run --project InternetVotingApplication.csproj --launch-profile "https (SQL Server)"`.

### Tryby bazy danych

| `Database:Provider` | Zachowanie |
| --- | --- |
| `SqlServer` (domyślnie) | SQL Server z `ConnectionStrings:InternetVotingDBConnection`. W Development, gdy `Database:FallbackToSqliteWhenUnavailable=true` i sonda `master` nie odpowie w `Database:ProbeTimeout`, aplikacja przechodzi na SQLite. Poza Development nigdy nie ma sondy ani fallbacku: brak bazy to czytelny błąd i kod wyjścia 1. |
| `Sqlite` | Plik z `Database:SqliteConnectionString` (domyślnie `App_Data/voting-dev.db`, ścieżka względem katalogu projektu). Schemat powstaje przez `EnsureCreated`, bez migracji. Tylko do rozwoju. |

Ważne przy SQLite: dane z pliku nie trafiają do SQL Servera (po powrocie SQL Servera „znikną", bo to inna
baza); `EnsureCreated` nie aktualizuje istniejącego pliku po zmianie modelu danych, wtedy usuń
`App_Data/voting-dev.db`. Zmienna `IVAPP_SKIP_DB_PROBE=1` wyłącza sondę. `dotnet ef` zawsze pracuje na
SQL Serverze (tryb projektowy pomija sondę).

Inna instancja SQL Server (Express, LocalDB) bez zmieniania plików w repozytorium:

```bash
dotnet user-secrets set "ConnectionStrings:InternetVotingDBConnection" "Server=localhost\SQLEXPRESS;Database=InternetVoting;Trusted_Connection=True;TrustServerCertificate=True;"
```

### Rozwiązywanie problemów

| Objaw | Przyczyna | Co zrobić |
| --- | --- | --- |
| `A compatible .NET SDK was not found` / `global.json` | brak SDK 10.0 | `winget install Microsoft.DotNet.SDK.10`, restart Visual Studio (wymagane 2022 17.14+ lub 2026) |
| żółty pasek „Tryb zapasowy" mimo zainstalowanego SQL Servera | usługa zatrzymana | `services.msc` → „SQL Server (MSSQLSERVER)" → Uruchom; restart aplikacji |
| pasek „Tryb zapasowy", w logu kod 2/53/26 | inna nazwa instancji lub wyłączony TCP/IP | `Server=localhost\SQLEXPRESS` lub `(localdb)\MSSQLLocalDB` w user secrets; SQL Server Configuration Manager → Protocols → TCP/IP |
| w logu kod 18456 | konto Windows bez loginu na serwerze | w SSMS dodaj login dla konta Windows albo użyj loginu SQL |
| w logu kod 4060 | brak bazy i brak uprawnień do jej utworzenia | nadaj koncie rolę `dbcreator` albo utwórz pustą bazę `InternetVoting` |
| dane „zniknęły" po włączeniu SQL Servera | wcześniej pracowałeś na SQLite | to inna baza; zarejestruj się ponownie albo wróć profilem `https (SQLite)` |
| `SQLite Error 1: no such column` | stary plik SQLite po zmianie modelu | usuń `App_Data/voting-dev.db` |
| brak e-maila aktywacyjnego | poczta idzie do plików | otwórz najnowszy plik z `App_Data/mail/` |
| przeglądarka ostrzega o certyfikacie | brak zaufanego certyfikatu deweloperskiego | `dotnet dev-certs https --trust` albo profil `http` |
| port 5001 zajęty | inna aplikacja | zmień `applicationUrl` w `Properties/launchSettings.json` |
| po utracie `App_Data/signing-key.pem` weryfikacja podpisów nie przechodzi | nowy klucz | przywróć kopię pliku; bez niej stare bloki są niepodpisane poprawnie |

Wariant z Dockerem (SQL Server + Mailpit na <http://localhost:8025> + aplikacja na <http://localhost:8080>):

```bash
docker compose up --build
```

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
| `Database:Provider`, `Database:SqliteConnectionString`, `Database:FallbackToSqliteWhenUnavailable`, `Database:ProbeTimeout` | silnik bazy, plik SQLite, tryb zapasowy (tylko Development) i czas sondy |
| `Database:ApplyMigrationsOnStartup` | stosowanie migracji przy starcie (`true` w Development, tylko SQL Server) |
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

Projekt używa aktualnych konwencji C# 14 / .NET 10: przestrzenie nazw w zapisie plikowym, konstruktory
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

- `GET /health` sprawdza połączenie z bazą; `GET /setup` (tylko Development) pokazuje pełną diagnostykę uruchomienia.
- Logi w formacie Serilog na konsolę, z logowaniem żądań HTTP; poziomy w sekcji `Serilog`.
- Dziennik audytu (`/Admin/Audit`) zapisuje działania administratorów, wyniki weryfikacji i kotwice.

## Licencja

MIT, patrz `LICENSE`.
