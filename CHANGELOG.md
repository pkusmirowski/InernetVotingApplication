# Historia zmian

Format zbliżony do [Keep a Changelog](https://keepachangelog.com/pl/1.1.0/). Wersja 1.x to pierwotna wersja pracy
inżynierskiej (.NET 6, sesja jako mechanizm logowania, łańcuch hashy bez podpisów). Wersja 2.0.0 to przebudowa opisana
w `docs/`.

## [2.2.0] - 2026-09-29

Wersja finalna.

### Wybory prezydenckie

- Aplikacja wraca do założenia pracy inżynierskiej: nazwa „Wybory Prezydenckie” w nagłówku, tytule stron,
  stopce i nadawcy wiadomości; dane przykładowe (tylko Development) to wybory prezydenckie: I i II tura 2025
  oraz nadchodzące wybory 2030.
- Mechanizm wyborów pozostaje ogólny, więc w rozwiniętej wersji można nim przeprowadzić także inne głosowania,
  np. referendum (opisane w README i `docs/OCENA_ZMIAN.md`).

### Porządek w kodzie

- Foldery zgodne z przestrzeniami nazw: modele formularzy w `ViewModels/`, walidatory i komunikaty walidacji
  w `Validation/`, szablony e-mail w `Services/Mail/EmailTemplates.cs`, role, polityki, nagłówki bezpieczeństwa
  i kontrola sesji w `Security/`, `AppVersion` w `Configuration/`; w katalogu głównym tylko `Program.cs`
  i `Startup.cs`. `PESELValidation.cs` → `PeselValidation.cs` (nazwa pliku jak nazwa klasy).
- Stałe zamiast powtarzanych napisów: wyzwalacze kontroli rejestru (`ChainService.Trigger*`), kody audytu kont
  testowych, klucze `TempData` (`StatusMessageExtensions`).
- Bez powielonego kodu: jedna metoda wydawania ciasteczka logowania, jeden blok wylogowania w menu; długie metody
  podzielone (reguły głosowania, budowa bloku, reguły zmiany dat).
- Usunięte resztki: nieużywane animacje CSS i profil IIS Express; zasady czystego kodu opisane w `docs/INSTRUKCJA.md`.
- `Aplikacja.cmd`: menu do włączania i wyłączania aplikacji bez Visual Studio; skrypty PowerShell zapisane z BOM,
  żeby polskie znaki wyświetlały się poprawnie w Windows PowerShell.

## [2.1.0] - 2026-09-29

### Panel administratora

- Edycja nazwy i dat wyborów (`/Admin/EditElection`) oraz usuwanie wyborów bez oddanych głosów razem z kandydatami,
  kotwicami i weryfikacjami; oba działania trafiają do dziennika audytu.
- Lista użytkowników (`/Admin/Users`): aktywacja konta bez e-maila, nadawanie i odbieranie roli administratora
  (bez odebrania jej sobie ani ostatniemu administratorowi).
- Konta testowe w Development (`Seeding:TestAccounts`): `admin@test.local` / `Admin123!` i `wyborca1..5@test.local` /
  `Wyborca123!`, aktywne od startu, wypisane na stronie `/setup`. Poza Development opcja jest wymuszona na wyłączoną,
  a konta testowe znalezione w bazie są przy starcie dezaktywowane i tracą rolę administratora.
- Profile startowe otwierają `/Setup` (wcześniej `Home/Setup`, adres nieistniejący).

### Język interfejsu

- Teksty dla wyborców napisane prostym językiem: „kod potwierdzenia” zamiast „hash”, „rejestr głosów” zamiast
  „łańcuch”, „kontrola rejestru” zamiast „weryfikacja”, „kopia kontrolna” zamiast „kotwica”. Dotyczy stron głosowania,
  wyszukiwarki głosu, wyników, rejestru głosów, strony głównej z FAQ, polityki prywatności, stron błędów, komunikatów
  formularzy i wiadomości e-mail; panel administratora używa tych samych nazw.
- Dane techniczne (hash, podpis, numer bloku, klucz publiczny) są nadal dostępne w rozwijanej sekcji
  „Szczegóły techniczne”.
- Komunikaty mówią, co zrobić dalej (np. wyszukiwarka głosu podaje, ile znaków ma wpisany kod), a strony błędów
  404, 403, 429 i 400 mają własne opisy.
- Komunikaty walidacji, które framework generował po angielsku (za długi tekst, błędna data lub liczba), są po polsku
  (`ValidationMessages`).
- Komunikaty o niepowodzeniu w panelu administratora są czerwone, a nie zielone (`StatusIsError`).
- Dziennik audytu pokazuje polskie opisy zdarzeń zamiast kodów; przed oddaniem głosu aplikacja pyta o potwierdzenie.

### Poprawki po przeglądzie (porównanie z wersją pierwotną, `docs/OCENA_ZMIAN.md`)

- Głos, który przegrał wyścig z innym głosem, jest ponawiany. W 2.0.0 kolejka poczty zapisywała blok przed obsługą
  konfliktu, więc zamiast ponowienia był błąd 500.
- Tajność głosu: wpis o udziale w wyborach ma tylko datę (dzień), nie ten sam znacznik czasu co blok; wiadomość,
  której nie udało się wysłać po ostatniej próbie, jest usuwana z kolejki (wcześniej zostawała z adresem i kodem).
- Kontrola rejestru czyta wybory i głosy w jednej transakcji, więc głos oddany w trakcie kontroli nie wywołuje
  fałszywego alarmu o naruszeniu.
- Panel administratora: kandydatów można dodawać i usuwać tylko przed rozpoczęciem głosowania; w trwających wyborach
  data rozpoczęcia jest zablokowana, a zakończenie można przesunąć najwcześniej na chwilę ostatniego głosu;
  zakończonych wyborów nie można ponownie otworzyć (tylko poprawić nazwę). Wyścig przy usuwaniu daje komunikat
  zamiast błędu 500.
- Brak wyników cząstkowych także dla administratora: liczba głosów na kandydata i eksport rejestru dopiero po
  zakończeniu wyborów.
- Sesje: każde żądanie zalogowanego użytkownika jest sprawdzane z bazą. Nadanie lub odebranie roli administratora
  działa od razu, a zmiana hasła i dezaktywacja konta kończą inne sesje (bieżąca sesja po zmianie hasła zostaje).
- Tokeny resetu hasła są zapisywane w bazie jako hash; link w wiadomości działa jak wcześniej.
- Strona błędu zwraca 429 także dla zablokowanego przez limit żądania POST bez tokenu (wcześniej 400) i przyjmuje
  tylko kody 400–599.
- Poza Development `Signing:AutoGenerateKey` jest wymuszone na wyłączone (brak klucza zatrzymuje start zamiast
  po cichu tworzyć nowy, z którym stare głosy się nie weryfikują).

### Porządki

- Usunięte nieużywane pliki: puste migracje `init2`/`init3`, `GeneratePassword`, `ArrayExtensions`,
  `GlosowanieWyborczeItemComparer`, `Properties/serviceDependencies.json`, biblioteki i obrazy szablonu
  (`isotope-layout`, `glightbox`, `php-email-form`, `img/portfolio`, `img/clients`, `img/team`); katalog `.vs/`
  usunięty z repozytorium.
- Konta testowe i klucz `Seeding:TestAccounts` opisane w `docs/INSTRUKCJA.md`.

### Wersja finalna

- Nazwa aplikacji „Głosowanie internetowe” zamiast „Wybory Prezydenckie” (aplikacja obsługuje dowolne głosowania).
- Kontakt tylko z konfiguracji (`App:ContactEmail`); fikcyjny adres, telefon i e-mail usunięte ze stopki i strony
  Kontakt. Bez skonfigurowanego adresu sekcja kontaktu jest ukryta.
- Komunikaty o kopiach kontrolnych mówią o wysyłce do komisji tylko wtedy, gdy są skonfigurowani odbiorcy.
- Czcionki systemowe zamiast Google Fonts (żadne dane odwiedzających nie trafiają do Google, zgodnie z polityką
  prywatności); usunięte biblioteki AOS i Boxicons, nieużywane pliki Bootstrapa i obrazy szablonu (`wwwroot` z 10 MB
  do niecałego 1 MB) oraz martwy kod szablonu w `main.js` i `style.css`.
- Pasek „tryb SQLite” tylko w Development; stopka pokazuje linki logowania tylko niezalogowanym.
- Poprawione okno potwierdzenia usuwania wyborów (tekst ucinany przez cudzysłów).
- Domyślna baza `InternetVotingV2`: pierwotna wersja pracy używała nazwy `InternetVoting` z innym schematem. Błąd 2714
  (tabele z innej wersji) ma własne wyjaśnienie przy starcie.
- E-mail z kodem głosu zawiera link do wyszukiwarki, gdy ustawiono `App:PublicBaseUrl`.
- Usunięte pliki Dockera (nieużywane) i robocze dokumenty planów; licencja w dokumentacji zgodna z plikiem
  `LICENSE` (GNU GPL v3); wymagane Visual Studio 2026.
- Testy: 311 → 315, pokrycie 99,2% linii i 91,7% gałęzi, powyżej progu CI (97% linii, 88% gałęzi).

## [2.0.0] - 2026-09-28

### Platforma

- .NET 10 LTS (C# 14), EF Core 10, ASP.NET Core 10; `global.json` pinuje SDK 10; obrazy Dockera `sdk:10.0` / `aspnet:10.0`.
- Nowoczesny styl kodu: przestrzenie nazw w zapisie plikowym, konstruktory podstawowe, rekordy, wyrażenia kolekcji,
  `TimeProvider`, `GeneratedRegex`, `Nullable`, `ImplicitUsings`; reguły w `.editorconfig` egzekwowane przez `dotnet format`.
- Usunięte nieużywane pakiety (Abp, Azure Key Vault, pakiety testowe w projekcie webowym); MailKit podniesiony
  z wersji z podatnością.

### Bezpieczeństwo

- Uwierzytelnianie cookie ASP.NET Core z rolami `Voter`/`Admin`, `[Authorize]` i polityką `AdminOnly`
  (wcześniej: stringi w sesji, akcja POST dodania kandydata bez sprawdzenia uprawnień).
- Globalna walidacja tokenu anty-CSRF; logowanie i wylogowanie tylko przez POST (wcześniej logowanie przez GET
  z hasłem w adresie).
- Blokada konta po nieudanych próbach, reset hasła jednorazowym linkiem z terminem ważności (wcześniej nowe hasło
  wysyłane jawnym tekstem każdemu, kto znał e-mail i PESEL), stała czasowo weryfikacja hasła.
- Ograniczenie liczby żądań na logowaniu, rejestracji i odzyskiwaniu hasła; nagłówki bezpieczeństwa (CSP, nosniff,
  frame-ancestors); dane SMTP i connection string poza kodem (user secrets / zmienne środowiskowe).
- Po przeglądzie bezpieczeństwa (2026-09-28): linki aktywacyjne i resetu hasła budowane z `App:PublicBaseUrl`
  zamiast z nagłówka `Host` (ochrona przed podmianą domeny w linku), automatyczny awans pierwszego konta na
  administratora i dane przykładowe wymuszone na „wyłączone" poza Development, eksport JSON łańcucha (z identyfikatorami
  kandydatów) publiczny dopiero po zakończeniu wyborów, wysłane wiadomości usuwane z outboxa (brak trwałego
  powiązania e-mail → hash głosu), potwierdzenie usunięcia kandydata przeniesione z atrybutu inline do `site.js`
  (zgodność z CSP).

### Głosowanie i łańcuch

- Oddanie głosu w transakcji `Serializable` z walidacją okna czasowego, przynależności kandydata i jednego głosu;
  unikalne indeksy w bazie; wyścigi rozstrzygane tokenem współbieżności z ponowieniem.
- Blok z indeksem, znacznikiem czasu, losowym nonce i separatorami pól; podpis ECDSA P-256 kluczem poza bazą;
  stan głowy łańcucha w wierszu wyborów (głos czyta dwa wiersze zamiast całego łańcucha).
- Pełna weryfikacja w tle z dziennikiem, kotwice (podpisane migawki głowy) e-mailem do komisji, publiczna strona
  łańcucha i eksport JSON, niezależny weryfikator `tools/ChainVerifier`.
- Poprawione: procent głosów liczony dzieleniem całkowitym (zawsze 0 lub 100), niewidoczne komunikaty o błędach,
  brak strony błędu, zepsute ścieżki zasobów, brak jQuery i walidacji klienckiej, złe przekierowania.

### Administracja i eksploatacja

- Panel wyborów (stan głowy, weryfikacja na żądanie, kotwica), usuwanie kandydatów bez głosów, dziennik audytu.
- Outbox poczty (tabela + dispatcher z ponawianiem) zamiast wysyłki w żądaniu; `/health`; Serilog.

### Uruchamianie

- Profile Visual Studio `https (SQL Server)` i `https (SQLite)`, strona diagnostyczna `/setup` (tylko Development),
  sonda SQL Servera z trybem zapasowym SQLite w Development, e-maile do plików w `App_Data/mail`, dane przykładowe,
  pierwsze aktywowane konto jako administrator, `run.cmd`/`run.ps1`, `docker-compose.yml` z SQL Server i Mailpit.

### Testy i CI

- 200+ testów: jednostkowe, serwisowe na SQLite w pamięci, kontrolerów (NSubstitute), integracyjne
  (`WebApplicationFactory`) z pełnym przebiegiem wyborcy i administratora; pokrycie ok. 92% kodu produkcyjnego.
- GitHub Actions: `dotnet format --verify-no-changes`, build, testy z pokryciem.

### Baza danych

- Nowa migracja `InitialCreate` (poprzednie migracje z 2023 r. zamienione na puste operacje; do usunięcia przez autora).
  Istniejącą bazę z wersji 1.x trzeba utworzyć od nowa: schemat i format bloków są niezgodne.

## [1.0.0] - 2023

- Pierwotna wersja: ASP.NET Core 6 MVC, EF Core, SQL Server, rejestracja z PESEL, aktywacja e-mailem, głosowanie,
  łańcuch hashy SHA-256 bez podpisów, wyszukiwarka głosu po hashu.
