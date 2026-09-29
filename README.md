# InternetVotingApplication

Aplikacja webowa do głosowań internetowych (praca inżynierska). Każdy oddany głos jest blokiem
w podpisanym łańcuchu hashy. Wyborca dostaje hash swojego głosu i może w każdej chwili sprawdzić,
czy głos znajduje się w nienaruszonym łańcuchu.

Technologie: .NET 10, ASP.NET Core MVC, Entity Framework Core, SQL Server, Bootstrap 5, xUnit.

## Co potrafi

- rejestracja z walidacją PESEL, aktywacja konta e-mailem, logowanie z rolami wyborca / administrator,
- głosowanie (jeden głos na wybory), potwierdzenie z hashem, wyniki po zakończeniu wyborów,
- łańcuch głosów podpisany kluczem ECDSA, weryfikacja w tle, publiczna strona łańcucha i eksport JSON,
- panel administratora: wybory, kandydaci, weryfikacja łańcucha, dziennik audytu.

## Uruchomienie

Potrzebne: [.NET SDK 10](https://dotnet.microsoft.com/download/dotnet/10.0) i Visual Studio 2022 17.14+ (lub 2026).
SQL Server jest opcjonalny: bez niego aplikacja w trybie Development uruchomi się na pliku SQLite.

1. Otwórz `InternetVotingApplication.sln`.
2. Wybierz profil **`https (SQL Server)`** i naciśnij **F5**.
3. Otworzy się strona `/setup`, która pokazuje stan aplikacji i co zrobić dalej.

Bez Visual Studio: dwuklik w `run.cmd`. Testy: `dotnet test` albo `run-tests.cmd`.

E-maile w trybie Development trafiają do plików w `App_Data/mail/`. Pierwsze aktywowane konto zostaje administratorem.

## Dokumentacja

- [`docs/INSTRUKCJA.md`](docs/INSTRUKCJA.md): konfiguracja, tryby bazy danych, rozwiązywanie problemów, testy, opis łańcucha głosów.
- [`docs/ARCHITEKTURA.md`](docs/ARCHITEKTURA.md): architektura systemu.
- [`CHANGELOG.md`](CHANGELOG.md): historia zmian.

## Licencja

MIT, patrz `LICENSE`.
