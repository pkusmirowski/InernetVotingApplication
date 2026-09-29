# Plan finalizacji projektu

Data: 2026-09-28. Cel: wersja projektu gotowa do oddania jako praca inżynierska: aktualny framework LTS,
zweryfikowana na docelowej platformie, z kompletną dokumentacją i czystą historią zmian.

| # | Zadanie | Kryterium ukończenia | Status |
| --- | --- | --- | --- |
| 1 | Migracja na **.NET 10 LTS** (wsparcie do listopada 2028; .NET 9 traci wsparcie 10 listopada 2026): trzy projekty, pakiety 10.x, `global.json`, obrazy Dockera, CI | build bez ostrzeżeń, snapshot EF Core 10 | wykonane |
| 2 | Weryfikacja na prawdziwym .NET 10 (nie na zastępczym SDK 8): build Release, wszystkie testy, pokrycie, `dotnet format` | wszystko zielone na SDK 10 | wykonane |
| 3 | Uruchomienie aplikacji „na żywo" i przejście przez strony (`/setup`, `/health`, strona główna, rejestracja) na .NET 10 | odpowiedzi HTTP 200, baner i diagnostyka poprawne | wykonane |
| 4 | Przegląd bezpieczeństwa całej gałęzi i usunięcie realnych ustaleń: linki e-mail z nagłówka `Host` (przejęcie konta przez podmianę domeny w linku resetu), awans pierwszego konta na administratora poza Development, publiczny eksport z wynikami cząstkowymi trwających wyborów, trwałe powiązanie e-mail → hash w outboxie, potwierdzenie usunięcia kandydata blokowane przez CSP | brak otwartych ustaleń wysokiego i średniego ryzyka; testy na każdą poprawkę | wykonane |
| 5 | Numer wersji aplikacji (`2.0.0`) widoczny w stopce i na stronie diagnostycznej | wersja z metadanych zestawu | wykonane |
| 6 | Dokumentacja końcowa: README (wersje, szybki start), `CHANGELOG.md` z pełną listą zmian od wersji pierwotnej, aktualizacja `docs/` | dokumenty opisują stan po zmianach | wykonane |
| 7 | Commit, push, opis pull requesta [pkusmirowski/InernetVotingApplication#2](https://github.com/pkusmirowski/InernetVotingApplication/pull/2) z podsumowaniem całości | PR aktualny | wykonane |
| 8 | **Po stronie autora**: usunięcie plików, których narzędzie nie mogło skasować (lista niżej), pierwsze F5 na PC, scalenie PR | | pliki usunięte 2026-09-29 (build i 217 testów zielone); F5 na PC z SQL Serverem do zrobienia ręcznie |

## Pliki do ręcznego usunięcia (usunięte 2026-09-29)

Wszystkie są nieużywane lub zneutralizowane; ich usunięcie nie zmienia zachowania aplikacji:

- `Migrations/20230619161930_init2.cs`, `Migrations/20230619161930_init2.Designer.cs`
- `Migrations/20230619163326_init3.cs`, `Migrations/20230619163326_init3.Designer.cs`
- `ExtensionMethods/GeneratePassword.cs`, `ExtensionMethods/ArrayExtensions.cs`, `ExtensionMethods/GlosowanieWyborczeItemComparer.cs`
- `Properties/serviceDependencies.json`
- `wwwroot/assets/vendor/isotope-layout/`, `wwwroot/assets/vendor/glightbox/`, `wwwroot/assets/vendor/php-email-form/`,
  `wwwroot/assets/img/portfolio/`, `wwwroot/assets/img/clients/`, `wwwroot/assets/img/team/`
- `.vs/` z indeksu gita: `git rm -r --cached .vs`

Po usunięciu: `dotnet build`, `dotnet test`, commit.

## Wymagania po stronie autora

- .NET SDK 10.0 (`winget install Microsoft.DotNet.SDK.10`), Visual Studio 2022 17.14+ lub Visual Studio 2026.
- SQL Server (dowolna edycja) albo praca w trybie SQLite (profil `https (SQLite)`).
- Zmiana hasła konta SMTP, które było w historii repozytorium.
