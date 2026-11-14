using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PledgeDesk.Api.Domain;
using PledgeDesk.Api.Services;
using PledgeDesk.Api.Tests.TestHelpers;

namespace PledgeDesk.Api.Tests.Services;

public class SettlementServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly Data.PledgeDeskDbContext _db;
    private readonly FakeClock _clock = new();
    private readonly SettlementService _sut;

    public SettlementServiceTests()
    {
        (_db, _connection) = SqliteTestDbContextFactory.Create();
        _sut = new SettlementService(_db, _clock);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<(Campaign Campaign, User Backer)> SeedActiveCampaignWithPledgeAsync(decimal goal, decimal pledgeAmount)
    {
        var creator = new User { Email = "creator@test.dev", DisplayName = "Creator", Role = UserRole.Creator };
        var backer = new User { Email = "backer@test.dev", DisplayName = "Backer", Role = UserRole.Backer };
        _db.Users.AddRange(creator, backer);
        await _db.SaveChangesAsync();

        var campaign = new Campaign
        {
            CreatorId = creator.Id,
            Title = "T",
            Category = "Tech",
            GoalAmount = goal,
            DeadlineUtc = _clock.UtcNow.AddDays(10),
            Status = CampaignStatus.Active,
            CreatedAtUtc = _clock.UtcNow
        };
        _db.Campaigns.Add(campaign);
        await _db.SaveChangesAsync();

        _db.Pledges.Add(new Pledge
        {
            CampaignId = campaign.Id,
            BackerId = backer.Id,
            Amount = pledgeAmount,
            Status = PledgeStatus.Confirmed,
            CreatedAtUtc = _clock.UtcNow
        });
        await _db.SaveChangesAsync();

        return (campaign, backer);
    }

    [Fact]
    public async Task RunSettlementAsync_IgnoresCampaignsBeforeDeadline()
    {
        await SeedActiveCampaignWithPledgeAsync(goal: 100m, pledgeAmount: 200m);

        var summary = await _sut.RunSettlementAsync();

        Assert.Equal(0, summary.CampaignsFunded);
        Assert.Equal(0, summary.CampaignsFailed);
    }

    [Fact]
    public async Task RunSettlementAsync_WhenGoalMet_MarksCampaignFunded()
    {
        var (campaign, _) = await SeedActiveCampaignWithPledgeAsync(goal: 100m, pledgeAmount: 150m);
        _clock.UtcNow = campaign.DeadlineUtc.AddMinutes(1);

        var summary = await _sut.RunSettlementAsync();

        Assert.Equal(1, summary.CampaignsFunded);
        var reloaded = await _db.Campaigns.FindAsync(campaign.Id);
        Assert.Equal(CampaignStatus.Funded, reloaded!.Status);
    }

    [Fact]
    public async Task RunSettlementAsync_WhenGoalNotMet_RefundsConfirmedPledges()
    {
        var (campaign, _) = await SeedActiveCampaignWithPledgeAsync(goal: 1000m, pledgeAmount: 100m);
        _clock.UtcNow = campaign.DeadlineUtc.AddMinutes(1);

        var summary = await _sut.RunSettlementAsync();

        Assert.Equal(1, summary.CampaignsFailed);
        Assert.Equal(1, summary.PledgesRefunded);

        var pledge = await _db.Pledges.FirstAsync(p => p.CampaignId == campaign.Id);
        Assert.Equal(PledgeStatus.Refunded, pledge.Status);
        Assert.NotNull(pledge.RefundedAtUtc);
    }

    [Fact]
    public async Task RunSettlementAsync_RunTwice_IsIdempotent()
    {
        var (campaign, _) = await SeedActiveCampaignWithPledgeAsync(goal: 1000m, pledgeAmount: 100m);
        _clock.UtcNow = campaign.DeadlineUtc.AddMinutes(1);

        var first = await _sut.RunSettlementAsync();
        var second = await _sut.RunSettlementAsync();

        Assert.Equal(1, first.PledgesRefunded);
        Assert.Equal(0, second.CampaignsFailed);
        Assert.Equal(0, second.PledgesRefunded);

        var pledge = await _db.Pledges.FirstAsync(p => p.CampaignId == campaign.Id);
        Assert.Equal(PledgeStatus.Refunded, pledge.Status);
    }
}
