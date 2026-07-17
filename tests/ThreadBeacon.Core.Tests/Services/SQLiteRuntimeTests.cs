using Microsoft.Data.Sqlite;

namespace ThreadBeacon.Core.Tests.Services;

public sealed class SQLiteRuntimeTests
{
    [Fact]
    public void NativeRuntime_MeetsSecurityBaseline()
    {
        using var connection = new SqliteConnection("Data Source=:memory:;Pooling=False");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT sqlite_version();";

        var version = Version.Parse(Assert.IsType<string>(command.ExecuteScalar()));

        Assert.True(version >= new Version(3, 50, 2), $"SQLite {version} is below the security baseline.");
    }
}
