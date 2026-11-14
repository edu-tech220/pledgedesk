using PledgeDesk.Api.Services;

namespace PledgeDesk.Api.Tests.TestHelpers;

public class FakeClock : IClock
{
    public DateTime UtcNow { get; set; } = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
