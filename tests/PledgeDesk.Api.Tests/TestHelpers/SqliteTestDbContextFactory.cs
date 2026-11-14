using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PledgeDesk.Api.Data;

namespace PledgeDesk.Api.Tests.TestHelpers;

/// <summary>
/// Creates an isolated PledgeDeskDbContext backed by a fresh SQLite in-memory database.
/// The connection must stay open for the context's lifetime - SQLite drops an in-memory
/// database as soon as its last connection closes - so callers own disposing both.
/// </summary>
public static class SqliteTestDbContextFactory
{
    public static (PledgeDeskDbContext Context, SqliteConnection Connection) Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<PledgeDeskDbContext>().UseSqlite(connection).Options;
        var context = new PledgeDeskDbContext(options);
        context.Database.EnsureCreated();

        return (context, connection);
    }

    /// <summary>
    /// Opens a second context against the same connection/database, used to simulate a
    /// concurrent request racing the original context for the same row.
    /// </summary>
    public static PledgeDeskDbContext CreateSecondContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<PledgeDeskDbContext>().UseSqlite(connection).Options;
        return new PledgeDeskDbContext(options);
    }
}
