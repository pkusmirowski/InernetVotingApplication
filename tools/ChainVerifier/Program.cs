using ChainVerifier;

if (args.Length != 1)
{
    Console.Error.WriteLine("Użycie: ChainVerifier <plik-eksportu.json>");
    Console.Error.WriteLine("Eksport pobierasz z aplikacji: /Election/Export/{id}");
    return 2;
}

var export = Verifier.Parse(await File.ReadAllTextAsync(args[0]));
var report = Verifier.Verify(export);

Console.WriteLine($"Wybory: {export.Election.Name} (id {export.Election.Id})");
Console.WriteLine($"Klucz: {export.KeyId}");
Console.WriteLine($"Bloki: {report.BlockCount}, hash głowy: {report.HeadHash ?? "(pusty)"}");
Console.WriteLine($"Kotwice zgodne z łańcuchem: {report.AnchorsMatched}/{export.Anchors.Count}");
Console.WriteLine("Rozkład głosów:");
foreach (var (candidate, votes) in report.Tally.OrderByDescending(x => x.Value))
{
    Console.WriteLine($"  {candidate}: {votes}");
}

if (report.IsValid)
{
    Console.WriteLine("WYNIK: łańcuch poprawny.");
    return 0;
}

Console.WriteLine($"WYNIK: łańcuch NIEPOPRAWNY ({report.Errors.Count} błędów):");
foreach (var error in report.Errors)
{
    Console.WriteLine($"  - {error}");
}

return 1;
