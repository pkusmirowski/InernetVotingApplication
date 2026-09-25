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

## Uruchomienie lokalne

Wymagania: .NET SDK 9.0, SQL Server (lub Docker).

1. Baza danych i serwer SMTP do testów przez Docker:

   ```bash
   docker compose up -d db mailpit
   ```

   SQL Server nasłuchuje na `localhost:1433` (użytkownik `sa`, hasło `Voting!Passw0rd`),
   a Mailpit odbiera pocztę na porcie 1025 i pokazuje ją pod <http://localhost:8025>.
   `appsettings.Development.json` jest już skonfigurowany pod te usługi.

2. Klucz podpisu bloków: w środowisku `Development` klucz jest generowany automatycznie do
   `App_Data/signing-key.pem` (plik jest w `.gitignore`). Zrób jego kopię: bloki podpisane tym kluczem
   nie zweryfikują się bez niego. W produkcji podaj klucz przez `Signing:PrivateKeyPem` (PKCS#8 PEM)
   z magazynu sekretów; wygenerujesz go np. tak:

   ```bash
   openssl ecparam -name prime256v1 -genkey -noout | openssl pkcs8 -topk8 -nocrypt
   ```

3. Uruchomienie aplikacji (w środowisku `Development` migracje są stosowane automatycznie):

   ```bash
   dotnet run --project InternetVotingApplication.csproj
   ```

   Alternatywnie ręcznie: `dotnet ef database update`.

4. Konto administratora: zarejestruj konto i aktywuj je linkiem z e-maila, a następnie wpisz jego adres
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
```

Testy nie wymagają SQL Server ani SMTP: serwisy i testy integracyjne działają na SQLite in-memory,
a poczta jest przechwytywana przez `FakeEmailSender`.

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
