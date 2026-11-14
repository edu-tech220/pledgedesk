namespace PledgeDesk.Api.Services;

/// <summary>
/// Thin abstraction over the current time so deadline/settlement logic can be tested
/// deterministically instead of racing against the real system clock.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
