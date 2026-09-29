# Historia zmian

Format zbliżony do [Keep a Changelog](https://keepachangelog.com/pl/1.1.0/). Wersja 1.x to pierwotna wersja pracy
inżynierskiej (.NET 6, sesja jako mechanizm logowania, łańcuch hashy bez podpisów). Wersja 2.0.0 to przebudowa opisana
w `docs/`.

## [Unreleased]

### Panel administratora

- Edycja nazwy i dat wyborów (`/Admin/EditElection`) oraz usuwanie wyborów bez oddanych głosów razem z kandydatami,
  kotwicami i weryfikacjami; oba działania trafiają do dziennika audytu.
- Lista użytkowników (`/Admin/Users`): aktywacja konta bez e-maila, nadawanie i odbieranie roli administratora
  (bez odebrania jej sobie ani ostatniemu administratorowi).
- Konta testowe w Development (`Seeding:TestAccounts`): `admin@test.local` / `Admin123!` i `wyborca1..5@test.local` /
  `Wyborca123!`, aktywne od startu, wypisane na stronie `/setup`. Poza Development opcja jest wymuszona na wyłączoną.
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
