using InternetVotingApplication.Data;
using Microsoft.Data.SqlClient;

namespace InternetVotingApplication.Tests.Unit;

public class SqlServerProbeTests
{
    [Fact]
    public void Probe_connection_string_targets_master_with_timeout_and_no_pooling()
    {
        var cs = SqlServerProbe.BuildProbeConnectionString("Server=localhost;Database=InternetVoting;Trusted_Connection=True;", TimeSpan.FromSeconds(2.2));

        var builder = new SqlConnectionStringBuilder(cs);
        Assert.Equal("master", builder.InitialCatalog);
        Assert.Equal(3, builder.ConnectTimeout);
        Assert.False(builder.Pooling);
        Assert.Equal("localhost", builder.DataSource);
    }

    [Fact]
    public void Refused_port_is_reported_as_unreachable_quickly()
    {
        var probe = new SqlServerProbe();

        var result = probe.CanConnect("Server=127.0.0.1,1;Database=InternetVoting;Trusted_Connection=True;TrustServerCertificate=True;", TimeSpan.FromSeconds(1));

        Assert.False(result.Ok);
        Assert.False(string.IsNullOrEmpty(result.Error));
        Assert.True(result.Elapsed < TimeSpan.FromSeconds(20), $"probe took {result.Elapsed}");
    }

    [Fact]
    public void Malformed_connection_string_is_reported_not_thrown()
    {
        var result = new SqlServerProbe().CanConnect("this is not a connection string", TimeSpan.FromSeconds(1));

        Assert.False(result.Ok);
        Assert.NotNull(result.Error);
    }
}
