namespace InternetVotingApplication.Interfaces
{
    public interface IAuditLog
    {
        /// <summary>Appends an entry and saves it immediately (participates in an ambient transaction if one is open).</summary>
        Task LogAsync(string action, string? details = null, int? userId = null);
    }
}
