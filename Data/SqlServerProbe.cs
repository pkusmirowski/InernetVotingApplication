using System.Diagnostics;
using Microsoft.Data.SqlClient;

namespace InternetVotingApplication.Data;

public sealed class SqlServerProbe : ISqlServerProbe
{
    public ProbeResult CanConnect(string connectionString, TimeSpan timeout)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            using var connection = new SqlConnection(BuildProbeConnectionString(connectionString, timeout));
            connection.Open();
            return new ProbeResult(true, null, null, stopwatch.Elapsed);
        }
        catch (SqlException ex)
        {
            return new ProbeResult(false, FirstLine(ex.Message), ex.Number, stopwatch.Elapsed);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException or FormatException)
        {
            return new ProbeResult(false, FirstLine(ex.Message), null, stopwatch.Elapsed);
        }
    }

    /// <summary>
    /// Probes the <c>master</c> database: on the very first run the application database does not exist yet,
    /// and that must not look like an unreachable server.
    /// </summary>
    public static string BuildProbeConnectionString(string connectionString, TimeSpan timeout)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = "master",
            ConnectTimeout = Math.Clamp((int)Math.Ceiling(timeout.TotalSeconds), 1, 60),
            Pooling = false,
        };
        return builder.ConnectionString;
    }

    private static string FirstLine(string message)
    {
        var index = message.IndexOfAny(['\r', '\n']);
        return index < 0 ? message : message[..index];
    }
}
