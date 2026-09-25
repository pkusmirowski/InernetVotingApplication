# Analiza aplikacji InternetVotingApplication i plan rozwoju

Data analizy: 2026-09-25. Zakres: cały kod C#, widoki Razor, konfiguracja projektu, migracje EF, zasoby statyczne.
Analiza jest statyczna (czytanie kodu) plus próba kompilacji. W środowisku analizy nie było SDK .NET 9, więc projekt
skompilowano na SDK .NET 8 po tymczasowej zmianie `TargetFramework` na `net8.0` i usunięciu pakietu `Abp`
(jest net9-only i nieużywany). W tej konfiguracji build przechodzi bez błędów, z jednym ostrzeżeniem kompilatora
(CS7022, patrz sekcja 5) i ostrzeżeniem NuGet o podatności w `MailKit 4.11.0` (NU1902).

Legenda priorytetów: **[KRYTYCZNY]** luka bezpieczeństwa lub błąd psujący główną funkcję, **[WYSOKI]** błąd
funkcjonalny widoczny dla użytkownika, **[ŚREDNI]** jakość kodu / wydajność / utrzymanie, **[NISKI]** kosmetyka.

---

## 1. Ogólna ocena

Mocne strony:

- Czytelny podział na kontrolery, serwisy z interfejsami i DI, modele, view-modele.
- Hasła hashowane BCrypt, walidacja PESEL z sumą kontrolną, walidacja wieku, aktywacja konta e-mailem.
- Pomysł na łańcuch hashy głosów z weryfikacją integralności i "paragonem" (hash) dla wyborcy.
- Włączone analizatory (`EnableNETAnalyzers`, `EnforceCodeStyleInBuild`, Roslynator, AsyncFixer).

Najważniejsze słabości (rozwinięte niżej):

1. Autoryzacja oparta na ręcznych stringach w sesji zamiast mechanizmu uwierzytelniania ASP.NET Core. Kilka akcji
   POST w ogóle nie sprawdza uprawnień.
2. Oddanie głosu nie weryfikuje, czy kandydat należy do wyborów, czy wybory trwają, i nie jest chronione tokenem
   anty-CSRF. Możliwe podwójne głosowanie w wyścigu.
3. Sekrety (SMTP) w kodzie źródłowym i w historii gita.
4. Komunikaty o błędach w widokach nigdy się nie wyświetlają (porównanie stringa z `false`).
5. Procent głosów liczony dzieleniem całkowitym (wynik zawsze 0 albo 100).
6. Brak testów, brak migracji tworzącej schemat bazy, brak CI, mnóstwo nieużywanych pakietów.
7. Zepsute ścieżki do skryptów i CSS: JS szablonu ładuje się tylko na stronie głównej, jQuery i walidacja
   po stronie klienta w ogóle nie istnieją w `wwwroot`.

---

## 2. Bezpieczeństwo

### 2.1 [KRYTYCZNY] Brak prawdziwego uwierzytelniania i autoryzacji

Stan: logowanie zapisuje `Session["email"]` i `Session["Admin"]`, a każda akcja ręcznie sprawdza te klucze
(`Controllers/AccountController.cs`, `AdminController.cs`, `ElectionController.cs`). `Startup.cs` woła
`UseAuthorization()` bez `UseAuthentication()` i bez żadnego `[Authorize]`.

Konsekwencje:

- `AdminController.AddCandidateAsync` (POST) nie ma żadnego sprawdzenia sesji. Każdy, także niezalogowany,
  może dodać kandydata wysyłając POST na `/Admin/AddCandidate`.
- Panel admina sprawdza tylko istnienie stringa `Admin`; łatwo pominąć przy refaktoryzacji.
- Brak wylogowania po zmianie hasła, brak wygaszania sesji (domyślnie 20 min idle, nieskonfigurowane).

Zalecenie: cookie authentication (`AddAuthentication().AddCookie()`), `ClaimsPrincipal` z rolą `Admin`,
`[Authorize]` / `[Authorize(Roles = "Admin")]` na kontrolerach, `[AllowAnonymous]` tylko tam, gdzie trzeba.
Alternatywnie pełne ASP.NET Core Identity (gotowe: lockout, potwierdzenie e-mail, reset hasła, 2FA).

### 2.2 [KRYTYCZNY] Oddanie głosu można sfałszować lub zdublować

`ElectionController.VotingAddAsync` i `ElectionService.AddVote`:

- Nie sprawdza, czy `candidateId` należy do `electionId`. Można zagłosować na kandydata z innych wyborów,
  a nawet na nieistniejący identyfikator (wtedy błąd FK i 500).
- Nie sprawdza, czy wybory trwają. Kontrole dat są tylko w akcji GET `Voting`; ręczny POST omija je.
- `CheckIfVoted` jest wykonywane poza `lock`, więc dwa równoległe żądania tego samego użytkownika mogą oba przejść
  sprawdzenie i oba zapisać głos. `lock` statyczny działa tylko w jednym procesie (nie w farmie serwerów).
- Brak `[ValidateAntiForgeryToken]` i brak `@Html.AntiForgeryToken()` w formularzu. Możliwy CSRF: strona
  atakującego może oddać głos w imieniu zalogowanego użytkownika.
- Brak unikalnego indeksu na `GlosUzytkownika (id_uzytkownik, id_wybory)`, więc baza też nie chroni przed duplikatem.

Zalecenie: walidacja przynależności kandydata i okna czasowego w serwisie, transakcja z poziomem izolacji
`Serializable` lub unikalny indeks + obsługa `DbUpdateException`, token anty-CSRF globalnie
(`AutoValidateAntiforgeryTokenAttribute` jako filtr globalny).

### 2.3 [KRYTYCZNY] Sekrety w repozytorium

`ExtensionMethods/Email.cs` zawiera login i hasło SMTP na stałe. `appsettings.json` zawiera connection string
z nazwą maszyny deweloperskiej. Nawet jeśli to konto testowe (Ethereal), dane są w historii gita na zawsze.

Zalecenie: `IOptions<SmtpOptions>` z `appsettings` + User Secrets lokalnie, zmienne środowiskowe / Key Vault
w produkcji. Pakiety Azure Key Vault są już w csproj, ale nieużywane. Zrotować hasło SMTP.

### 2.4 [WYSOKI] Logowanie przyjmuje GET

`AccountController.LoginAsync` nie ma `[HttpPost]`, więc `GET /Account/Login?Email=..&Haslo=..` też loguje.
Hasło trafia do logów serwera, historii przeglądarki i nagłówka Referer. Brak `[ValidateAntiForgeryToken]`.
Brak ograniczenia liczby prób (lockout / rate limiting), więc możliwy brute force.

### 2.5 [WYSOKI] Odzyskiwanie hasła

`UserService.RecoverPassword`: znajomość e-maila i numeru PESEL (dane nietajne) pozwala każdemu zresetować hasło
innej osoby, co odcina ją od konta i wysyła nowe hasło na jej e-mail. Nowe hasło jest generowane `System.Random`
(nie kryptograficzny) i wysyłane jawnym tekstem.

Zalecenie: link resetujący z jednorazowym, ograniczonym czasowo tokenem (`RandomNumberGenerator`), hasło ustawia
sam użytkownik. Odpowiedź z formularza zawsze taka sama ("jeśli konto istnieje, wysłano e-mail"), by nie ujawniać
istnienia konta.

### 2.6 [WYSOKI] Aktywacja konta

`GetUserByAcitvationCode` po aktywacji ustawia kod na `Guid.Empty`. Wywołanie
`/Account/Activation/00000000-0000-0000-0000-000000000000` przy dwu lub więcej aktywowanych użytkownikach
rzuci `InvalidOperationException` z `SingleOrDefault` (500). Niepoprawny GUID w URL (`new Guid(string)`) też
kończy się wyjątkiem zamiast komunikatem. Kod powinien być `Guid?` ustawiany na `null`, a parametr akcji typu
`Guid id` z walidacją.

### 2.7 [ŚREDNI] Nadmiarowe wiązanie modelu (over-posting)

`RegisterAsync(Uzytkownik user)` wiąże encję bazodanową bezpośrednio z formularza, łącznie z `Id`,
`JestAktywne`, `KodAktywacyjny`. Serwis nadpisuje dwa ostatnie, ale wysłanie `Id` powoduje błąd identity insert.
To samo dotyczy `Kandydat` i `DataWyborow`. Zalecenie: osobne klasy DTO / ViewModel dla formularzy.

### 2.8 [ŚREDNI] Tajność głosu

Tabela `GlosowanieWyborcze` nie ma klucza użytkownika (dobrze), ale rekordy w `GlosowanieWyborcze`
i `GlosUzytkownika` są wstawiane parami w jednej transakcji, więc kolejność `id` w obu tabelach pozwala
administratorowi bazy jednoznacznie przypisać głos do osoby. Dodatkowo hash głosu jest deterministyczny
(kandydat, wybory, poprzedni hash), a wyszukiwarka jest publiczna, więc kto pozna hash, pozna głos
(problem "receipt-freeness": możliwość udowodnienia komuś, na kogo się głosowało, ułatwia kupowanie głosów).
Do pracy inżynierskiej warto to jawnie omówić jako ograniczenie i zaproponować kierunek (np. losowa sól/nonce
w bloku, opóźnione lub losowo przetasowane zapisywanie, ślepe tokeny uprawniające do głosu).

### 2.9 [NISKI] Pozostałe

- `Logout` przez GET (możliwe wylogowanie linkiem). Powinno być POST.
- Brak nagłówków bezpieczeństwa (CSP, X-Content-Type-Options, Referrer-Policy), brak `Cookie.SameSite/Secure`
  dla sesji.
- `MailKit 4.11.0` ma znaną podatność (GHSA-9j88-vvj5-vhgr). Podnieść do aktualnej wersji.
- `Microsoft.Azure.KeyVault 3.0.5` jest pakietem przestarzałym (deprecated).

---

## 3. Błędy funkcjonalne

### 3.1 [WYSOKI] Komunikaty o błędach nigdy się nie pokazują

Kontrolery ustawiają `ViewBag.Error = "Login failed..."` (string), a widoki sprawdzają `ViewBag.Error == false`
(Login, Register, PasswordRecovery, CreateElection, AddCandidate). String nigdy nie jest równy `false`, więc
użytkownik po nieudanym logowaniu widzi po prostu pusty formularz. Analogicznie `ViewBag.Success == true`
w PasswordRecovery nigdy nie jest prawdą. Zalecenie: `TempData`/`ViewBag` z jednym typem (string) i sprawdzanie
`!string.IsNullOrEmpty(...)`, lub `ModelState.AddModelError("", ...)` i `asp-validation-summary`.

### 3.2 [WYSOKI] Procent głosów liczony dzieleniem całkowitym

`ElectionService.GetElectionResult`: `(result.CountedVotes / allElectionVotes) * 100` to dzielenie `int/int`.
Wynik to 0 dla wszystkich kandydatów poza tym, który ma 100% głosów. Poprawka: `100.0 * CountedVotes / total`
i zaokrąglenie do wyświetlenia.

### 3.3 [WYSOKI] Zepsute ścieżki zasobów statycznych

`Views/Shared/_Layout.cshtml`:

- Skrypty szablonu mają ścieżki względne bez `/` (`assets/vendor/aos/aos.js`, `assets/js/main.js`).
  Na `/Account/Login` przeglądarka szuka `/Account/assets/js/main.js` i dostaje 404. Efekt: menu mobilne,
  animacje AOS itd. działają tylko na stronie głównej. Poprawka: `~/assets/...`.
- `~/lib/bootstrap/...`, `~/lib/jquery/...`, `~/css/site.css`, `~/js/site.js` wskazują na katalog `wwwroot/lib`,
  który nie istnieje. Bootstrap jest więc ładowany dwukrotnie z dwóch adresów (jeden 404).
- `_ValidationScriptsPartial` odwołuje się do jQuery Validation w `~/lib`, którego nie ma. Walidacja po stronie
  klienta nigdy nie działa, mimo że formularze ją "dołączają".
- `assets/vendor/swiper/*` jest wpisany w csproj i layoucie, ale katalog nie istnieje w repo.

Zalecenie: LibMan (`libman.json`) lub npm do pobierania bibliotek, usunięcie nieużywanych (isotope, glightbox,
php-email-form, swiper, boxicons) albo świadome zostawienie tylko potrzebnych.

### 3.4 [WYSOKI] Brak strony błędu

`Startup.Configure` używa `UseExceptionHandler("/Error")`, ale taka trasa nie istnieje (jest `/Home/Error`),
a widok `Error.cshtml` w ogóle nie istnieje. Każdy wyjątek w produkcji kończy się drugim błędem.
Dodać `Views/Shared/Error.cshtml`, `UseExceptionHandler("/Home/Error")` i `UseStatusCodePagesWithReExecute`.

### 3.5 [ŚREDNI] Przekierowania do nieistniejących akcji

`AccountController.Register` / `RegisterAsync` przy zalogowanym użytkowniku robi `RedirectToAction("Dashboard")`
w kontrolerze Account, gdzie nie ma takiej akcji (404). Powinno być `RedirectToAction("Dashboard", "Election")`.
`VotingAddAsync` przy już oddanym głosie przekierowuje do `ElectionResult` bez parametrów, więc `ver = 0`,
widok wchodzi w gałąź wyników z modelem `null` i pętla `foreach` rzuca `NullReferenceException` (500).

### 3.6 [ŚREDNI] Wyszukiwarka głosu

`AccountController.Search`: `text ??= "1"` to dziwny domyślny hash. Serwis dla nieznalezionego głosu ustawia
`IdKandydat = -1`, a widok sprawdza `!= 0`, więc zamiast komunikatu "nie znaleziono" renderuje tabelę z pustymi
komórkami. Formularz wysyła pole `Text`, ale wpisana wartość nie jest zachowywana w polu po wyszukaniu
(`Model.Text` nie jest ustawiane).

### 3.7 [ŚREDNI] Logika dat i mylące nazwy

`CheckIfElectionEnded` zwraca `true`, gdy wybory **jeszcze się nie skończyły** (`DataZakonczenia >= Now`).
Nazwa mówi coś odwrotnego. To działa tylko dlatego, że wszędzie jest negowane. Zmienić na `HasEnded` /
`IsOngoing`. Przy tworzeniu wyborów nikt nie sprawdza `DataZakonczenia > DataRozpoczecia`.

### 3.8 [ŚREDNI] Duplikaty kandydatów

`AdminService.AddCandidateAsync` sprawdza unikalność imienia i nazwiska globalnie, a nie w obrębie wyborów. Ta sama
osoba nie może startować w dwóch różnych wyborach, a dwie różne osoby o tym samym nazwisku nie mogą startować
w jednych.

### 3.9 [NISKI] Interfejs

- Lista kandydatów używa checkboxów z JS wymuszającym wyłączność zamiast `<input type="radio">`.
- W tabeli kandydatów wyświetlany jest surowy `Id` z bazy jako "Numer kandydata".
- Znacznik `<center>` (przestarzały), `</br>` (niepoprawny HTML), odstępy robione czterema `<br />`.
- Formularz kontaktowy w `Views/Home/Contact.cshtml` wysyła do `forms/contact.php` (pozostałość szablonu).
- Mieszane języki komunikatów: polskie w widokach, angielskie w kontrolerach ("Login failed. Please check...").
- Link "Sprawdź wyniki głosowania" i pozostałe warianty na Dashboardzie prowadzą do tej samej akcji `Voting`,
  która potem przekierowuje; czytelniej linkować od razu do właściwej akcji.

---

## 4. "Blockchain" - ocena rozwiązania

Obecna implementacja (`Blockchain/*.cs`) to liniowy łańcuch hashy SHA-256 przechowywany w tej samej bazie, co dane.
Warto w pracy nazwać to precyzyjnie ("hash chain / rejestr z weryfikacją integralności"), bo brakuje cech
kojarzonych z blockchainem: rozproszenia, konsensusu, podpisów, dowodu pracy lub innego mechanizmu utrudniającego
przepisanie historii.

Konkretne słabości:

1. **Kto ma dostęp do bazy, może przepisać cały łańcuch** i przeliczyć wszystkie hashe. Weryfikacja nic nie wykryje.
   Minimalne wzmocnienie: HMAC z kluczem serwera trzymanym poza bazą (Key Vault) albo podpis ECDSA każdego bloku,
   plus okresowa publikacja hasha "głowy" łańcucha (np. wysyłka e-mailem do komisji, wpis w innym systemie).
2. **Dane bloku sklejane bez separatorów**: `$"{candidateId}{electionId}{vote}{previousBlockHash}"`.
   Kandydat 1 w wyborach 12 daje ten sam tekst co kandydat 11 w wyborach 2. Użyć jednoznacznej serializacji
   (JSON, lub pola rozdzielone `|`) i dodać znacznik czasu oraz losowy nonce.
3. **Brak pola `Timestamp`, `Nonce`, numeru bloku (`Index`)** - standardowe elementy bloku, które warto mieć
   choćby dla opisu w pracy.
4. **Wydajność**: `VerifyElectionBlockchain` ładuje wszystkie głosy wyborów do pamięci przy każdym wejściu
   na stronę i przy każdym oddaniu głosu, a `SingleOrDefault` w pętli daje złożoność O(n²). Przy kilku tysiącach
   głosów będzie to zauważalne. Weryfikować przyrostowo (tylko nowy blok względem zapamiętanego hasha głowy),
   a pełną weryfikację uruchamiać w tle / na żądanie. Szukać poprzednika w słowniku `Dictionary<int, ...>`.
5. **Poprzedni blok wybierany bez `OrderBy`**: `listOfPreviousElectionVotes.LastOrDefault()` opiera się na
   kolejności zwróconej przez SQL Server bez `ORDER BY`, co nie jest gwarantowane. Użyć
   `OrderByDescending(x => x.Id).FirstOrDefault()`.
6. **Kolumna `hash` to `varchar(max)`** bez indeksu. Wyszukiwarka robi pełny skan tabeli. Zmienić na `char(64)`
   z indeksem unikalnym.

Kierunki rozwoju w duchu pracy: drzewo Merkle na wybory z publikowanym korzeniem, podpisy bloków, symulacja
kilku węzłów weryfikujących (np. drugi serwis/proces), eksport łańcucha do pliku audytowego, porównanie z realnym
rozwiązaniem (np. Hyperledger, Ethereum testnet) w części teoretycznej.

---

## 5. Konfiguracja projektu i zależności

- **[WYSOKI] Pakiety testowe w projekcie webowym**: `Microsoft.NET.Test.Sdk`, `NUnit`, `NUnit3TestAdapter`,
  `xunit`, `xunit.runner.visualstudio`, `Moq`, `AutoFixture`, `Microsoft.AspNetCore.Mvc.Testing`,
  `Microsoft.AspNetCore.TestHost`, `Microsoft.EntityFrameworkCore.InMemory`. Katalog z testami został usunięty
  (commit "Delete InternetVotingApplicationTests directory"), więc testów jest zero, a pakiety generują drugi
  punkt wejścia (ostrzeżenie CS7022) i puchną w publikacji. Przenieść do osobnego projektu `*.Tests`.
- **[WYSOKI] Nieużywane pakiety**: `Abp` (framework aplikacyjny, ani jedno użycie), `Azure.*`, `Microsoft.Azure.KeyVault`
  (deprecated), `Microsoft.VisualStudio.Web.CodeGeneration.Design`, `Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation`
  (nie włączone w `Startup`). Usunąć lub zacząć używać (Key Vault dla sekretów ma sens).
- **[ŚREDNI] Migracje**: są tylko `init2` i `init3` zmieniające jedną kolumnę; brak migracji tworzącej schemat.
  Snapshot ma `ProductVersion 6.0.8`. Bazy nie da się odtworzyć z repozytorium, więc nikt (promotor, recenzent)
  nie uruchomi aplikacji. Wygenerować `InitialCreate` od zera (lub dodać skrypt SQL) i `docker-compose.yml`
  z SQL Server.
- **[ŚREDNI] `csproj`** zawiera ~100 wpisów `<None Include="wwwroot\...">` wygenerowanych przez Visual Studio;
  są zbędne (SDK Web dołącza `wwwroot` automatycznie) i wskazują m.in. na nieistniejący `swiper`.
- **[ŚREDNI] `.vs/`** jest w `.gitignore`, ale dwa pliki zostały już scommitowane (`git rm -r --cached .vs`).
- **[ŚREDNI] `README.md`** mówi o .NET 6, projekt jest na .NET 9. Brak instrukcji uruchomienia, konfiguracji
  SMTP, tworzenia bazy, konta administratora (tabela `Administrator` nie ma żadnego seedu ani UI).
- **[NISKI] `Startup` + `Program`**: podwójny wzorzec; w .NET 9 czytelniej użyć samego `Program.cs` (minimal hosting).
  `AddRazorPages()` i `MapRazorPages()` bez żadnej strony Razor Pages.
- **[NISKI] Brak** `.editorconfig`, `Directory.Build.props`, `<Nullable>enable</Nullable>`,
  `<ImplicitUsings>enable</ImplicitUsings>`, CI (workflow GitHub Actions został usunięty), `Dockerfile`.

---

## 6. Jakość kodu i architektura

- **Magiczne liczby**: `loginResult` 0/1/2, `ver` 1/2/3, `Type` 1/2/3, `ifAdded.Length > 3`, `JestAktywne` 0/1
  (typu `int?` po dwóch migracjach z `bit`). Zamienić na `enum LoginResult`, `enum ElectionStatus`, `bool IsActive`,
  a `AddVote` niech zwraca `Result<string>` / rzuca wyjątek domenowy zamiast stringa "0".
- **Mieszanie sync i async EF**: `AddVote`, `ChangePassword`, `GetUserByAcitvationCode`, `GetAllElections`,
  `SearchVote`, `GetElectionResult` są synchroniczne, reszta async. Ujednolicić na async.
- **Wysyłka e-mail w żądaniu HTTP**, statyczna klasa, synchroniczny SMTP. Jeśli SMTP nie odpowiada, użytkownik
  po zapisanym głosie dostaje 500 i nie widzi hasha. Wprowadzić `IEmailSender` (DI, async, `IOptions<SmtpOptions>`),
  a wysyłkę przenieść do kolejki w tle (`Channel<T>` + `BackgroundService`) i obłożyć `try/catch` + log.
- **N+1 i nadmiar zapytań**:
  - `GetAllElections`: `GetElectionType(x.Id)` w projekcji robi 2 zapytania na każdy wiersz. Wystarczy porównać
    daty już pobranego wiersza w pamięci.
  - `GetElectionResult`: dla każdego głosu 3 zapytania (`GetCandidateInfo` x2, `CountVotes`), potem `Distinct`.
    Jedno zapytanie `GroupBy(IdKandydat)` z `Count()` i `Join` do kandydatów załatwia całość.
  - `AddVote`: dwa osobne zapytania o `Id` i `Email` tego samego użytkownika.
- **Nazewnictwo**: mieszanka polska/angielska (`Uzytkownik`, `GlosUzytkownika` vs `ElectionService`, `CandidateName`),
  literówka `GetUserByAcitvationCode`, `ShowElectionByName` zwraca wszystkie wybory, `SearchVote(string Text)`
  (parametr z wielkiej litery), `DataWyborowItemViewModel` w folderze `ViewModels` ale w przestrzeni nazw `Models`,
  klasa `ItemEqualityComparer` w pliku `GlosowanieWyborczeItemComparer.cs`, folder `ExtensionMethods` zawiera
  rzeczy niebędące extension methods (`Email`, `GeneratePassword`, `AgeAttribute`). Zdecydować się na jeden język
  (dla pracy inżynierskiej w Polsce polskie nazwy encji są akceptowalne, ale warto konsekwentnie).
- **Nieużywany kod**: `IUserService.AuthenticateUser`, `IAdminService` wstrzykiwany do `AccountController`,
  `Views/Admin/DeleteCandidate.cshtml` (pusty), `GlosowanieWyborczeItemViewModel.Glos`, kolumna
  `GlosUzytkownika.glos` (zawsze `true`).
- **Encje jako modele formularzy** z atrybutami walidacyjnymi i `[NotMapped] ConfirmPassword` w encji
  `Uzytkownik`. Rozdzielić: encje bez atrybutów UI, osobne `RegisterViewModel`, `CandidateFormModel` itd.
  Opcjonalnie FluentValidation.
- **Brak logowania** (`ILogger`) w całej aplikacji, brak obsługi wyjątków w serwisach, brak health checków.
- **Schemat bazy**: brak unikalnych indeksów na `Uzytkownik.email` i `Uzytkownik.pesel` (unikalność sprawdzana
  tylko w kodzie, wyścig możliwy), `hash` i `opis` jako `varchar(max)`, `Pesel` `IsFixedLength` ale `StringLength(11)`
  bez `MinimumLength`. `DeleteBehavior.ClientSetNull` na FK niepustych (`int`) nie ma sensu; użyć `Restrict`.
- **Widoki**: kontrole logowania powtórzone w każdej akcji zamiast filtra/atrybutu; `ViewBag` zamiast silnie
  typowanych modeli; logika statusu wyborów (1/2/3) w widoku.

---

## 7. Proponowany plan działania

### Etap 0 - szybkie poprawki (1-2 dni, nie zmieniają architektury)

1. Naprawić warunki `ViewBag.Error == false` / `ViewBag.Success == true` we wszystkich widokach.
2. Dzielenie zmiennoprzecinkowe w procentach wyników.
3. Ścieżki `~/assets/...` w layoucie, usunąć odwołania do `~/lib` i `swiper` albo dodać biblioteki przez LibMan.
4. `[HttpPost]` + `[ValidateAntiForgeryToken]` na Login, VotingAdd, AddCandidate, CreateElection, PasswordRecovery,
   Logout; sprawdzenie admina w `AddCandidateAsync`.
5. Walidacja w `AddVote`: kandydat należy do wyborów, wybory trwają, użytkownik nie głosował (w transakcji).
6. `RedirectToAction("Dashboard", "Election")` w `AccountController`.
7. Poprawić `KodAktywacyjny` na `Guid?` i obsłużyć niepoprawny GUID.
8. Sekrety SMTP do konfiguracji (`IOptions`), rotacja hasła, connection string do User Secrets.
9. Usunąć nieużywane pakiety, `.vs/` z repo, wpisy `<None Include>` z csproj, dodać `Error.cshtml`.
10. Zaktualizować README (.NET 9, jak uruchomić, jak stworzyć admina).

### Etap 1 - fundamenty jakości (tydzień)

1. Projekt `InternetVotingApplication.Tests` (xUnit): testy `PeselValidation`, `AgeAttribute`, `HashHelper`,
   `BlockChainHelper` (łańcuch poprawny, zmodyfikowany blok, usunięty blok), serwisów na SQLite in-memory,
   testy integracyjne kontrolerów przez `WebApplicationFactory`.
2. GitHub Actions: `dotnet build`, `dotnet test`, `dotnet format --verify-no-changes`.
3. Migracja `InitialCreate`, `docker-compose.yml` (SQL Server + aplikacja), seed konta administratora.
4. `Nullable` + `ImplicitUsings`, `.editorconfig`, minimal hosting w `Program.cs`.
5. Cookie authentication z rolami, `[Authorize]`, usunięcie ręcznych sprawdzeń sesji.
6. `IEmailSender` async z kolejką w tle i logowaniem.
7. Enumy zamiast magicznych liczb, DTO zamiast encji w formularzach.

### Etap 2 - rozwój merytoryczny (materiał do rozdziałów pracy)

1. **Blok z pełną strukturą**: `Index`, `Timestamp`, `Nonce`, `Data` (kanoniczny JSON), `PreviousHash`, `Hash`,
   `Signature` (ECDSA kluczem serwera). Weryfikacja podpisu i łańcucha; testy manipulacji.
2. **Drzewo Merkle / publikacja korzenia** po zakończeniu wyborów; strona publiczna z "dowodem włączenia" głosu.
3. **Rozdzielenie uprawnienia od głosu**: użytkownik po zalogowaniu dostaje jednorazowy, losowy token
   uprawniający, którym oddaje głos; tabela głosów nie zapisuje niczego powiązanego z osobą ani kolejnością
   (np. opóźnione zapisywanie w losowej kolejności). Omówić kompromis weryfikowalność vs tajność.
4. **Panel administratora**: lista wyborów i kandydatów, edycja/usuwanie (istniejące TODO), zamykanie wyborów,
   eksport wyników i łańcucha do CSV/JSON, dziennik audytu działań admina.
5. **Wyniki**: wykres (np. Chart.js), frekwencja, wyniki dostępne dopiero po zakończeniu, także dla niezalogowanych.
6. **Bezpieczeństwo kont**: lockout po N próbach, 2FA e-mail/TOTP, reset hasła linkiem, polityka sesji, CSP.
7. **Lokalizacja** (`IStringLocalizer`, pliki `.resx` pl/en), dostępność (radio zamiast checkbox, etykiety, kontrast).
8. **Obserwowalność**: Serilog, health checks, metryki liczby głosów/wyborów.

---

## 8. Lista plików z konkretnymi miejscami do poprawy

| Plik | Co poprawić |
| --- | --- |
| `Controllers/AccountController.cs` | `[HttpPost]` na Login, anty-CSRF, redirect do `Election/Dashboard`, `Guid?` w aktywacji, usunąć `IAdminService`, `Logout` POST |
| `Controllers/AdminController.cs` | brak sprawdzenia admina w `AddCandidateAsync`, anty-CSRF, walidacja dat wyborów |
| `Controllers/ElectionController.cs` | anty-CSRF, walidacja kandydata/okna czasowego, `lock` nie obejmuje `CheckIfVoted`, magiczne `ver` i `Length > 3` |
| `Services/ElectionService.cs` | dzielenie całkowite, N+1, `LastOrDefault` bez `OrderBy`, mylące `CheckIfElectionEnded`, sync EF |
| `Services/UserService.cs` | reset hasła tokenem, `Random` -> `RandomNumberGenerator`, `Guid.Empty` |
| `Services/AdminService.cs` | unikalność kandydata per wybory, walidacja dat |
| `Blockchain/BlockHelper.cs` | separatory/serializacja, timestamp, nonce |
| `Blockchain/BlockChainHelper.cs` | O(n²), weryfikacja przyrostowa |
| `ExtensionMethods/Email.cs` | sekrety do konfiguracji, DI, async, kolejka |
| `ExtensionMethods/GeneratePassword.cs` | generator kryptograficzny |
| `Models/Uzytkownik.cs` | rozdzielić encję i model formularza, `bool IsActive`, `Guid? ActivationCode`, unikalne indeksy |
| `Models/InternetVotingContext.cs` | indeksy unikalne (email, pesel, hash, (user, election)), `char(64)` dla hash, `Restrict` |
| `Views/Shared/_Layout.cshtml` | ścieżki `~/assets`, usunąć `~/lib`, `swiper` |
| `Views/Shared/_ValidationScriptsPartial.cshtml` | dodać jQuery + jQuery Validation przez LibMan |
| `Views/Account/*.cshtml`, `Views/Admin/*.cshtml` | warunki `ViewBag.Error == false` |
| `Views/Election/Voting.cshtml` | radio zamiast checkbox, anty-CSRF |
| `Views/Account/Search.cshtml` | komunikat "nie znaleziono", zachowanie wpisanej wartości |
| `Views/Home/Contact.cshtml` | usunąć `forms/contact.php` lub podpiąć akcję |
| `InternetVotingApplication.csproj` | usunąć pakiety testowe i nieużywane, wpisy `<None>`, podnieść `MailKit`, `Nullable` |
| `Startup.cs` / `Program.cs` | uwierzytelnianie cookie, `/Home/Error`, opcje sesji, nagłówki bezpieczeństwa |
| `Migrations/` | `InitialCreate`, aktualny snapshot |
| `README.md` | aktualna wersja .NET, instrukcja uruchomienia |
