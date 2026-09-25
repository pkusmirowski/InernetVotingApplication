# Plan działania: etap 2 (architektura zaufania i eksploatacja)

Data: 2026-09-25. Plan wynika z `docs/ARCHITEKTURA.md` (sekcje 3 i 4). Każdy punkt ma kryterium
ukończenia, które jest sprawdzane testem lub stroną w aplikacji.

**Status: wszystkie 14 punktów wykonane** (84 testy, build i `dotnet format` czyste).

| # | Zadanie | Kryterium ukończenia |
| --- | --- | --- |
| 1 | **Stan głowy łańcucha** w `DataWyborow` (`HeadHash`, `LiczbaBlokow`, token współbieżności) i weryfikacja przyrostowa przy oddaniu głosu (tylko głowa + nowy blok) | oddanie głosu nie ładuje całego łańcucha; test wykrywa rozjazd głowy z blokami |
| 2 | **Podpis bloków ECDSA (P-256)** kluczem trzymanym poza bazą (`Signing:*`), publiczny klucz dostępny w aplikacji | każdy blok ma podpis; weryfikacja odrzuca blok z podmienionym podpisem; test |
| 3 | **Pełna weryfikacja w tle** (`ChainVerificationWorker`) z zapisem wyniku i czasu; strony wyników i wyszukiwarki pokazują zapisany wynik; admin może uruchomić ręcznie | worker działa cyklicznie; wynik widoczny na stronach; test serwisu |
| 4 | **Kotwiczenie hasha głowy** (`IChainAnchor`): rejestr kotwic + e-mail do komisji co N bloków i po zakończeniu wyborów | tabela kotwic, e-mail w outboxie, test wyzwalania |
| 5 | **Publiczna strona łańcucha** (`/Election/Chain/{id}`) i **eksport JSON** (`/Election/Export/{id}`) z kluczem publicznym | strona i JSON dostępne bez logowania; test integracyjny |
| 6 | **Niezależny weryfikator offline** (`tools/ChainVerifier`, konsola) sprawdzający eksport bez kodu aplikacji | weryfikuje poprawny eksport i wykrywa manipulację; test |
| 7 | **Outbox dla poczty** (tabela `WiadomoscEmail`, dispatcher z ponawianiem) zamiast kolejki w pamięci | wiadomość zapisana w tej samej transakcji co głos; test dispatchera |
| 8 | **Dziennik audytu** działań administratora i zdarzeń łańcucha, widok w panelu | wpisy przy tworzeniu wyborów, kandydatach, weryfikacji, kotwicach |
| 9 | **Panel wyborów dla administratora** (`/Admin/Elections`): głowa, liczba bloków, wynik weryfikacji, kotwice, przyciski "weryfikuj" i "opublikuj kotwicę" | strona działa, akcje POST z CSRF |
| 10 | **Ograniczenie liczby żądań** (rate limiting) na logowaniu, rejestracji, odzyskiwaniu hasła | polityka aktywna; test integracyjny nie przekracza limitu |
| 11 | **Health check** `/health` z kontrolą bazy, **Serilog** z logowaniem żądań | endpoint zwraca `Healthy`; logi strukturalne |
| 12 | **Rozbicie `ElectionService`** na `ElectionService` (lista, głosowanie), `ResultsService` (wyniki, wyszukiwanie) i `ChainService` (głowa, weryfikacja, kotwice, eksport) | każdy serwis ma jedną odpowiedzialność; testy przechodzą |
| 13 | **Migracja** odzwierciedlająca nowy schemat (regenerowana `InitialCreate`, bo poprzednia nie została jeszcze wdrożona) | `dotnet ef migrations` generuje pełny schemat |
| 14 | **Dokumentacja**: README (klucz podpisu, kotwice, weryfikacja offline, health), aktualizacja `ARCHITEKTURA.md` i `ANALIZA_I_PLAN_ROZWOJU.md` | dokumenty opisują stan po zmianach |

Świadomie **poza planem** (z uzasadnieniem):

- **Tokeny urny / rozdzielenie tożsamości od głosu.** Sam token nie usuwa powiązania wyborca → głos, bo
  w jednej bazie pozostaje korelacja czasowa i kolejności zapisu (patrz `ARCHITEKTURA.md` 3.2). Rzetelne
  rozwiązanie wymaga mieszania (mixnet, opóźniony zapis w losowej kolejności) albo kryptografii
  (ślepe podpisy, szyfrowanie homomorficzne). Zostawiam to jako opisany kierunek badawczy, zamiast
  wdrażać mechanizm, który daje pozorne bezpieczeństwo.
- **2FA i lokalizacja interfejsu**: funkcje produktowe, nie architektoniczne; nie wpływają na tezę pracy.
- **Wykresy JS**: polityka CSP `script-src 'self'` i brak dostępu do CDN w tym środowisku; wyniki mają
  paski procentowe w CSS.
