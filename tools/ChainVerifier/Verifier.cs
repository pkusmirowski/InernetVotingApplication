using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ChainVerifier
{
    /// <summary>
    /// Independent re-implementation of the chain rules. Deliberately shares no code with the web application so
    /// that the export format, not the application, is what an observer has to trust.
    /// </summary>
    public static class Verifier
    {
        private const string TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss.fffffff";

        public static VerificationReport Verify(ChainExport export)
        {
            ArgumentNullException.ThrowIfNull(export);
            var report = new VerificationReport();

            if (export.Format != "internet-voting-chain/v2")
            {
                report.Errors.Add($"Nieznany format eksportu: {export.Format}");
                return report;
            }

            using var key = ECDsa.Create();
            try
            {
                key.ImportFromPem(export.PublicKeyPem);
            }
            catch (CryptographicException ex)
            {
                report.Errors.Add($"Nie można wczytać klucza publicznego: {ex.Message}");
                return report;
            }

            var keyId = Convert.ToHexString(SHA256.HashData(key.ExportSubjectPublicKeyInfo()))[..16];
            if (!string.Equals(keyId, export.KeyId, StringComparison.OrdinalIgnoreCase))
            {
                report.Errors.Add($"Identyfikator klucza {export.KeyId} nie odpowiada kluczowi publicznemu ({keyId})");
            }

            var blocks = export.Blocks.OrderBy(b => b.Index).ToList();
            string? previousHash = null;
            for (int i = 0; i < blocks.Count; i++)
            {
                var b = blocks[i];
                if (b.Index != i)
                {
                    report.Errors.Add($"Blok {i}: oczekiwano indeksu {i}, jest {b.Index}");
                }

                if (b.ElectionId != export.Election.Id)
                {
                    report.Errors.Add($"Blok {i}: należy do wyborów {b.ElectionId}, a eksport dotyczy {export.Election.Id}");
                }

                if (!string.Equals(b.PreviousHash ?? string.Empty, previousHash ?? string.Empty, StringComparison.OrdinalIgnoreCase))
                {
                    report.Errors.Add($"Blok {i}: hash poprzedniego bloku nie zgadza się");
                }

                var data = string.Join('|', "v2", b.Index, b.CandidateId, b.ElectionId,
                    b.Timestamp.ToString(TimestampFormat, CultureInfo.InvariantCulture), b.Nonce, previousHash ?? string.Empty);
                var expected = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(data)));
                if (!string.Equals(expected, b.Hash, StringComparison.OrdinalIgnoreCase))
                {
                    report.Errors.Add($"Blok {i}: hash {b.Hash} różni się od obliczonego {expected}");
                }

                if (!VerifySignature(key, b.Hash, b.Signature))
                {
                    report.Errors.Add($"Blok {i}: niepoprawny podpis");
                }

                if (export.Candidates.All(c => c.Id != b.CandidateId))
                {
                    report.Errors.Add($"Blok {i}: kandydat {b.CandidateId} nie występuje na liście kandydatów");
                }

                previousHash = b.Hash;
            }

            report.BlockCount = blocks.Count;
            report.HeadHash = previousHash;

            if (export.Election.BlockCount != blocks.Count)
            {
                report.Errors.Add($"Nagłówek deklaruje {export.Election.BlockCount} bloków, w eksporcie jest {blocks.Count}");
            }

            if (!string.Equals(export.Election.HeadHash ?? string.Empty, previousHash ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            {
                report.Errors.Add("Hash głowy z nagłówka nie zgadza się z ostatnim blokiem");
            }

            foreach (var anchor in export.Anchors)
            {
                var data = string.Join('|', "anchor-v1", export.Election.Id, anchor.BlockCount, anchor.HeadHash ?? string.Empty,
                    anchor.Date.ToString(TimestampFormat, CultureInfo.InvariantCulture));
                if (!VerifySignature(key, data, anchor.Signature))
                {
                    report.Errors.Add($"Kotwica z {anchor.Date:g}: niepoprawny podpis");
                    continue;
                }

                if (anchor.BlockCount > blocks.Count)
                {
                    report.Errors.Add($"Kotwica z {anchor.Date:g} deklaruje {anchor.BlockCount} bloków, łańcuch ma {blocks.Count}: historia została skrócona");
                }
                else if (anchor.BlockCount > 0 && !string.Equals(blocks[anchor.BlockCount - 1].Hash, anchor.HeadHash, StringComparison.OrdinalIgnoreCase))
                {
                    report.Errors.Add($"Kotwica z {anchor.Date:g}: blok {anchor.BlockCount - 1} ma inny hash niż zakotwiczony: historia została przepisana");
                }
                else
                {
                    report.AnchorsMatched++;
                }
            }

            report.Tally = blocks
                .GroupBy(b => b.CandidateId)
                .ToDictionary(
                    g => export.Candidates.FirstOrDefault(c => c.Id == g.Key) is { } c ? $"{c.FirstName} {c.LastName}" : $"#{g.Key}",
                    g => g.Count());

            return report;
        }

        public static ChainExport Parse(string json)
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
            return JsonSerializer.Deserialize<ChainExport>(json, options)
                ?? throw new InvalidDataException("Pusty eksport.");
        }

        private static bool VerifySignature(ECDsa key, string data, string signatureBase64)
        {
            try
            {
                var signature = Convert.FromBase64String(signatureBase64);
                return key.VerifyData(Encoding.UTF8.GetBytes(data), signature, HashAlgorithmName.SHA256, DSASignatureFormat.Rfc3279DerSequence);
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }

    public sealed class VerificationReport
    {
        public List<string> Errors { get; } = [];

        public bool IsValid => Errors.Count == 0;

        public int BlockCount { get; set; }

        public string? HeadHash { get; set; }

        public int AnchorsMatched { get; set; }

        public Dictionary<string, int> Tally { get; set; } = [];
    }

    public sealed record ChainExport(
        string Format,
        DateTime ExportedAt,
        ChainExportElection Election,
        string KeyId,
        string PublicKeyPem,
        IReadOnlyList<ChainExportCandidate> Candidates,
        IReadOnlyList<ChainExportBlock> Blocks,
        IReadOnlyList<ChainExportAnchor> Anchors);

    public sealed record ChainExportElection(int Id, string Name, DateTime Start, DateTime End, int BlockCount, string? HeadHash);

    public sealed record ChainExportCandidate(int Id, string FirstName, string LastName);

    public sealed record ChainExportBlock(
        int Index,
        int CandidateId,
        int ElectionId,
        DateTime Timestamp,
        string Nonce,
        string? PreviousHash,
        string Hash,
        string Signature,
        string KeyId);

    public sealed record ChainExportAnchor(DateTime Date, int BlockCount, string? HeadHash, string Reason, string Signature);
}
