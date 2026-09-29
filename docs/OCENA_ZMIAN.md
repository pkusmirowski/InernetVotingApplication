# Ocena przebudowy względem wersji pierwotnej

Data: 2026-09-29. Porównanie ostatniej wersji autora (commit `7266c79` z 2025-03-19, „Minor changes”) ze stanem po
przebudowie (commity od `ec4febf`) i po przeglądzie poprawności opisanym niżej.

## 1. Idea pracy i czy została zachowana

Pierwotna aplikacja pokazuje głosowanie internetowe, w którym każdy oddany głos jest blokiem w wewnętrznym łańcuchu
hashy (osobnym dla każdych wyborów). Zmiana zapisanego głosu przerywa łańcuch, a łańcuch jest sprawdzany przed
głosowaniem i przed pokazaniem wyników. Tożsamość wyborcy (`Uzytkownik`, `GlosUzytkownika`) jest trzymana osobno
od anonimowych głosów (`GlosowanieWyborcze`). Wyborca dostaje hash swojego głosu i może go sprawdzić w publicznej
wyszukiwarce.

**Ta idea jest nadal rdzeniem aplikacji.** Głos to nadal wiersz `GlosowanieWyborcze` powiązany z poprzednim przez
`IdPoprzednie` i hash SHA-256 (`Blockchain/BlockHelper.cs`, `Blockchain/HashHelper.cs`). Wyborca nadal dostaje hash
na ekranie i e-mailem i sprawdza go w `Account/Search`. Przebudowa wzmacnia tę samą koncepcję, nie zastępuje jej
inną.

## 2. Co zostało zachowane

| Element | Oryginał | Teraz |
| --- | --- | --- |
| Kontrolery | `Account`, `Admin`, `Election`, `Home` | te same, te same adresy stron |
| Serwisy | `IUserService`, `IElectionService`, `IAdminService` | te same interfejsy; wyniki i łańcuch wydzielone do `ResultsService` i `ChainService` |
| Encje i tabele | `Uzytkownik`, `Kandydat`, `DataWyborow`, `GlosowanieWyborcze`, `GlosUzytkownika`, `Administrator` | te same polskie nazwy tabel i kolumn; `Administrator` nadal decyduje o roli |
| Łańcuch | `Blockchain/BlockChainHelper.cs`, `BlockHelper.cs`, `HashHelper.cs` | te same pliki, SHA-256, wielkie litery hex |
| Walidacja PESEL | `ExtensionMethods/PESELValidation.cs` | logika bez zmian |
| Wiek 18–120 | `AgeAttribute` | bez zmian, komunikat po polsku |
| Wygląd | szablon Bootstrap „Ninestars”, polskie etykiety | ten sam szablon i `style.css`; teksty prostszym językiem |
| Widoki | strony konta, panelu, głosowania i strony główne | wszystkie pod tymi samymi ścieżkami |

## 3. Błędy wersji pierwotnej, które przebudowa usunęła

Odnośniki wskazują pliki w commicie `7266c79`.

| Problem w oryginale | Gdzie | Teraz |
| --- | --- | --- |
| Login i hasło SMTP wpisane w kod | `ExtensionMethods/Email.cs:14-15` | konfiguracja i user secrets; poczta przez kolejkę |
| Dodawanie kandydata bez sprawdzenia uprawnień | `Controllers/AdminController.cs:48` | `[Authorize(Policy = AdminOnly)]` na całym kontrolerze |
| Podwójny głos przez wyścig (sprawdzenie poza `lock`, brak unikalnego indeksu) | `Controllers/ElectionController.cs:84-90` | unikalny indeks (wyborca, wybory) i transakcja |
| Brak kontroli terminu i przynależności kandydata przy wysłaniu głosu | `Controllers/ElectionController.cs:69-101` | sprawdzane w transakcji głosu |
| CSRF na większości formularzy, logowanie przez GET | `Controllers/AccountController.cs:50` | globalny `AutoValidateAntiforgeryToken`, logowanie tylko POST |
| Reset hasła: e-mail + PESEL od razu zmieniały hasło, nowe hasło jawnie e-mailem, `System.Random` | `Services/UserService.cs:98-113`, `ExtensionMethods/GeneratePassword.cs` | jednorazowy link z terminem ważności |
| Autoryzacja przez klucz sesji `"Admin"` | `AccountController.cs` | uwierzytelnianie ciasteczkiem i role |
| Kolizje treści bloku (`"{kandydat}{wybory}..."`: 1+12 i 11+2 dają ten sam tekst), niska entropia | `Blockchain/BlockHelper.cs:15` | pola rozdzielone i wersjonowane, losowy nonce 128 bitów, podpis ECDSA |
| Weryfikacja O(n²) przy każdym żądaniu | `Blockchain/BlockChainHelper.cs:18` | O(n), stan głowy, pełna kontrola w tle |
| Procenty w wynikach zawsze 0 (dzielenie całkowite) | `Services/ElectionService.cs:211` | poprawne procenty |
| Wyjątek przy porównaniu `ViewBag.Error == false` z tekstem | `Views/Account/Login.cshtml:33` i inne | modele widoków zamiast `ViewBag` |
| `NullReferenceException` na stronie wyników po ponownym głosie | `ElectionController.cs:86` | jeden parametr `id`, bez kodów `ver` |
| Awaria przy błędnym identyfikatorze aktywacji | `AccountController.cs:121` | parametr `Guid?` |
| Wstrzyknięcie HTML w e-mailu (imię) | `ExtensionMethods/Email.cs:19` | wartości kodowane |
| Usuwanie kandydata tylko jako pusty widok („do zrobienia”) | `Views/Admin/DeleteCandidate.cshtml` | zaimplementowane |

## 4. Co doszło ponad oryginał i czy to ma sens

Aplikacja pozostaje systemem do głosowania w wyborach prezydenckich. Rozwinięciem jest to, że mechanizm wyborów jest
ogólny: administrator może utworzyć dowolne wybory z własną listą kandydatów, więc da się nią przeprowadzić także
drugą turę albo inne głosowanie, np. referendum.

| Dodatek | Ocena |
| --- | --- |
| Podpisy ECDSA, stan głowy, kotwice wysyłane poza system, niezależny weryfikator `tools/ChainVerifier` | wzmacnia główną tezę (wykrywalność manipulacji); rozbudowane, ale spójne z ideą |
| 315 testów (jednostkowe, serwisy na SQLite, integracyjne), pokrycie 99% linii i 91% gałęzi, próg pokrycia w CI | duża wartość; wcześniej testów nie było |
| Panel: edycja i usuwanie wyborów, użytkownicy i role, dziennik audytu | uzupełnia brakujące funkcje |
| Skrypty uruchomieniowe, strona diagnostyczna, konta testowe | wygoda uruchomienia |
| Kolejka poczty (outbox) i strona diagnostyczna `/setup` | przerośnięte jak na skalę projektu, ale działają i są przetestowane; zostawione |

Z pierwotnego README zniknęło pięć zrzutów ekranu. Pokazują stary wygląd, więc nie zostały przywrócone.

## 5. Błędy wprowadzone przez przebudowę i naprawione w tym przeglądzie

Przegląd 2026-09-29 znalazł błędy, które pojawiły się dopiero w przebudowie. Wszystkie są naprawione i mają testy:

1. **Ponowienie głosu nie działało.** Kolejka poczty zapisywała blok przed obsługą konfliktu, więc dwa równoczesne
   głosy w tych samych wyborach kończyły się błędem 500 zamiast ponowienia (`Services/ElectionService.cs`).
2. **Powiązanie wyborca → głos.** Blok i wpis o udziale dostawały ten sam znacznik czasu co do tyknięcia, więc jedno
   złączenie w bazie pokazywało, kto na kogo głosował. Wpis o udziale ma teraz tylko datę. Wiadomości z kodem
   potwierdzenia, których nie udało się wysłać, zostawały w bazie na zawsze; teraz są usuwane.
3. **Fałszywy alarm o naruszeniu łańcucha.** Kontrola czytała wybory i bloki osobno; głos oddany pomiędzy odczytami
   wyglądał jak manipulacja (`Services/ChainService.cs`).
4. **Zmiany w trakcie głosowania.** Administrator mógł dodać lub usunąć kandydata po oddaniu głosów, przesunąć
   rozpoczęcie trwających wyborów i ponownie otworzyć zakończone (`Services/AdminService.cs`).
5. **Wyniki cząstkowe dla administratora.** Lista kandydatów i eksport rejestru pokazywały liczbę głosów przed
   końcem wyborów.
6. **Konfiguracja produkcyjna.** Konta testowe o jawnych hasłach działały, jeśli baza z Development trafiła do
   produkcji; klucz podpisu mógł się po cichu wygenerować na nowo, blokując głosowanie.
7. **Sesje nie widziały zmian konta.** Odebrana rola administratora działała do wylogowania, a zmiana hasła nie
   kończyła innych sesji. Teraz każde żądanie zalogowanego użytkownika jest sprawdzane z bazą
   (`Services/SessionValidator.cs`).
8. **Tokeny resetu hasła w bazie jawnie.** Teraz zapisywany jest tylko ich hash.
9. **Strona błędu przy limicie żądań.** Zablokowane przez limit żądanie POST bez tokenu pokazywało błąd 400 zamiast
   429; adres strony błędu przyjmował też dowolny kod (np. 0 lub 999).

## 6. Znane ograniczenia

Świadomie pozostawione, bo aplikacja działa lokalnie (opis w `docs/ARCHITEKTURA.md`, sekcje 3.2 i 4b):

- **Tajność głosu.** Kolejność identyfikatorów bloku i wpisu o udziale nadal pozwala je powiązać osobie z dostępem
  do bazy. To ograniczenie istniało już w wersji pierwotnej. Pełne rozwiązanie wymaga osobnego tokenu urny albo
  mieszania, czyli zmiany samego sposobu głosowania.
- Za reverse proxy potrzebne byłoby `UseForwardedHeaders`; lokalnie nie ma proxy.
- Wymiana klucza podpisu wymaga ponownego podpisania łańcuchów.
- Czas lokalny serwera zamiast UTC.

## 7. Jak to sprawdzono (2026-09-29)

- `dotnet build -c Release`: 0 ostrzeżeń i 0 błędów; `dotnet format --verify-no-changes`: bez zmian.
- `dotnet test`: 315 z 315; pokrycie 99,2% linii i 91,7% gałęzi (wcześniej 91,1% i 77,5%), próg w CI: 97% i 88%. Testy dla błędów z punktu 5 najpierw uruchomiono na kodzie sprzed poprawki: nie
  przechodziły. Po poprawce przechodzą.
- Pełny przebieg na nowej, pustej bazie SQLite (aplikacja w trybie Development, żądania HTTP), 48 z 48 kroków:
  - rejestracja: zły PESEL, wiek poniżej 18 lat i zajęty e-mail odrzucone; logowanie przed aktywacją odrzucone;
  - konto: aktywacja linkiem z e-maila, zmiana hasła (bieżąca sesja zostaje), stare hasło odrzucone, odzyskanie
    hasła linkiem (link działa raz), blokada konta po 5 błędnych hasłach;
  - wybory: administrator tworzy wybory i dodaje kandydatów przed startem; po starcie dodanie kandydata i zmiana
    daty rozpoczęcia są odrzucone;
  - głosowanie: 4 głosy dają 4 różne kody, drugi głos tego samego wyborcy odrzucony, kod odnajduje się
    w wyszukiwarce (wielkość liter bez znaczenia), eksport i liczby głosów ukryte przed końcem, także przed
    administratorem;
  - role: nadanie i odebranie roli administratora działa od razu w otwartej sesji;
  - koniec wyborów: zakończenie przed ostatnim głosem odrzucone; po zamknięciu wyniki 50 / 25 / 25 %, eksport
    dostępny, niezależny `tools/ChainVerifier` potwierdza łańcuch i kopię kontrolną; ponowne otwarcie odrzucone,
    ręczna kontrola rejestru bez zastrzeżeń;
  - limit żądań: po 20 próbach logowania w minucie odpowiedź 429;
  - dziennik aplikacji bez błędów.
- Nie sprawdzono tutaj: uruchomienia na SQL Serverze (na maszynie testowej nie ma SQL Servera; ta ścieżka ma testy,
  ale nie była uruchomiona).

## 8. Wniosek

Przebudowa jest rozwinięciem pracy, a nie inną aplikacją: ta sama idea łańcucha głosów z kodem dla wyborcy, te same
encje, kontrolery, walidacja i wygląd. Naprawia wszystkie poważne błędy bezpieczeństwa i poprawności oryginału,
a sam łańcuch wzmacnia podpisami, kotwicami i niezależnym weryfikatorem. W przebudowie pojawiło się kilka nowych
błędów; ten przegląd je usunął. Wszystkie funkcje przeszły testy i pełny przebieg opisany w punkcie 7.
Ograniczenia z punktu 6 są znane, opisane i świadomie pozostawione dla aplikacji uruchamianej lokalnie.
