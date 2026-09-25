namespace InternetVotingApplication.ViewModels
{
    /// <summary>
    /// Machine-readable export of one election chain. Everything needed to re-verify the chain without the
    /// application: block data, the public key and the candidate list. Field names are stable (documented in README).
    /// </summary>
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
