using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace PledgeDesk.Api.Data;

/// <summary>
/// Stores money as integer cents instead of a native decimal column. SQLite has no
/// decimal column type and cannot run SUM() over the TEXT-encoded values EF Core's
/// Sqlite provider uses for decimal by default; storing cents as a plain integer keeps
/// aggregate queries (pledged amount totals) working identically on SQL Server and the
/// SQLite database used in tests.
/// </summary>
public class MoneyConverter : ValueConverter<decimal, long>
{
    public MoneyConverter() : base(
        amount => (long)Math.Round(amount * 100m, 0, MidpointRounding.AwayFromZero),
        cents => cents / 100m)
    {
    }
}
