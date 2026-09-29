# InternetVotingApplication

Aplikacja webowa do głosowania w wyborach prezydenckich przez internet (praca inżynierska). Każdy oddany głos jest blokiem
w podpisanym łańcuchu hashy. Wyborca dostaje hash swojego głosu i może w każdej chwili sprawdzić,
czy głos znajduje się w nienaruszonym łańcuchu.

Technologie: .NET 10, ASP.NET Core MVC, Entity Framework Core, SQL Server, Bootstrap 5, xUnit.

## Co potrafi

- rejestracja z walidacją PESEL, aktywacja konta e-mailem, logowanie z rolami wyborca / administrator,
- głosowanie (jeden głos na wybory), potwierdzenie z hashem, wyniki po zakończeniu wyborów,
- łańcuch głosów podpisany kluczem ECDSA, weryfikacja w tle, publiczna strona łańcucha i eksport JSON,
- panel administratora: wybory (dodawanie, edycja, usuwanie), kandydaci, użytkownicy i role, weryfikacja łańcucha,
  dziennik audytu.

Aplikacja powstała do głosowania w wyborach prezydenckich. Mechanizm wyborów jest ogólny, więc w rozwiniętej wersji
można nim przeprowadzić także inne głosowania, np. kolejną turę wyborów albo referendum.

## Uruchomienie

Potrzebne: [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0) i Visual Studio 2026.
SQL Server jest opcjonalny: bez niego aplikacja w trybie Development uruchomi się na pliku SQLite.

1. Otwórz `InternetVotingApplication.sln`.
2. Wybierz profil **`https (SQL Server)`** i naciśnij **F5**.
3. Otworzy się strona `/setup`, która pokazuje stan aplikacji i co zrobić dalej.

Bez Visual Studio: dwuklik w `Aplikacja.cmd`, menu do włączania i wyłączania aplikacji (otwiera też przeglądarkę).
Testy: `dotnet test` albo `run-tests.cmd`.

E-maile w trybie Development trafiają do plików w `App_Data/mail/`. Konta testowe: `admin@test.local` / `Admin123!`
(administrator) i `wyborca1@test.local` … `wyborca5@test.local` / `Wyborca123!`.

## Dokumentacja

- [`docs/INSTRUKCJA.md`](docs/INSTRUKCJA.md): konfiguracja, tryby bazy danych, rozwiązywanie problemów, testy, opis łańcucha głosów.
- [`docs/ARCHITEKTURA.md`](docs/ARCHITEKTURA.md): architektura systemu.
- [`docs/OCENA_ZMIAN.md`](docs/OCENA_ZMIAN.md): porównanie z pierwotną wersją pracy i wyniki przeglądu.
- [`CHANGELOG.md`](CHANGELOG.md): historia zmian.

## Licencja

GNU GPL v3, patrz `LICENSE`.
