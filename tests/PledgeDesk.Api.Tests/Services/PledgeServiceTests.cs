using Microsoft.Data.Sqlite;
using PledgeDesk.Api.Domain;
using PledgeDesk.Api.Services;
using PledgeDesk.Api.Tests.TestHelpers;

namespace PledgeDesk.Api.Tests.Services;

public class PledgeServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly Data.PledgeDeskDbContext _db;
    private readonly FakeClock _clock = new();
    private readonly PledgeService _sut;

    public PledgeServiceTests()
    {
        (_db, _connection) = SqliteTestDbContextFactory.Create();
        _sut = new PledgeService(_db, _clock);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<Campaign> SeedActiveCampaignAsync(int? rewardStock = null, decimal minimumPledge = 10m)
    {
        var creator = new User { Email = "creator@test.dev", DisplayName = "Creator", Role = UserRole.Creator };
        _db.Users.Add(creator);
        await _db.SaveChangesAsync();

        var campaign = new Campaign
        {
            CreatorId = creator.Id,
            Title = "Gadget",
            Category = "Tech",
            GoalAmount = 1000m,
            DeadlineUtc = _clock.UtcNow.AddDays(10),
            Status = CampaignStatus.Active,
            CreatedAtUtc = _clock.UtcNow
        };
        _db.Campaigns.Add(campaign);
        await _db.SaveChangesAsync();

        if (rewardStock.HasValue)
        {
            var tier = new RewardTier
            {
                CampaignId = campaign.Id,
                Title = "Early bird",
                MinimumPledge = minimumPledge,
                TotalStock = rewardStock,
                ClaimedStock = 0
            };
            _db.RewardTiers.Add(tier);
            await _db.SaveChangesAsync();
        }

        return campaign;
    }

    private async Task<User> SeedBackerAsync(string email = "backer@test.dev")
    {
        var backer = new User { Email = email, DisplayName = "Backer", Role = UserRole.Backer };
        _db.Users.Add(backer);
        await _db.SaveChangesAsync();
        return backer;
    }

    [Fact]
    public async Task CreatePledgeAsync_WithoutRewardTier_Succeeds()
    {
        var campaign = await SeedActiveCampaignAsync();
        var backer = await SeedBackerAsync();

        var pledge = await _sut.CreatePledgeAsync(campaign.Id, backer.Id, 50m, rewardTierId: null);

        Assert.Equal(PledgeStatus.Confirmed, pledge.Status);
        Assert.Equal(50m, pledge.Amount);
    }

    [Fact]
    public async Task CreatePledgeAsync_OnDraftCampaign_Throws()
    {
        var campaign = await SeedActiveCampaignAsync();
        campaign.Status = CampaignStatus.Draft;
        await _db.SaveChangesAsync();
        var backer = await SeedBackerAsync();

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _sut.CreatePledgeAsync(campaign.Id, backer.Id, 50m, null));
        Assert.Equal("campaign_not_active", ex.ErrorCode);
    }

    [Fact]
    public async Task CreatePledgeAsync_PastDeadline_Throws()
    {
        var campaign = await SeedActiveCampaignAsync();
        _clock.UtcNow = campaign.DeadlineUtc.AddDays(1);
        var backer = await SeedBackerAsync();

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _sut.CreatePledgeAsync(campaign.Id, backer.Id, 50m, null));
        Assert.Equal("campaign_past_deadline", ex.ErrorCode);
    }

    [Fact]
    public async Task CreatePledgeAsync_BelowRewardTierMinimum_Throws()
    {
        var campaign = await SeedActiveCampaignAsync(rewardStock: 5, minimumPledge: 25m);
        var tierId = campaign.RewardTiers.Single().Id;
        var backer = await SeedBackerAsync();

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _sut.CreatePledgeAsync(campaign.Id, backer.Id, 10m, tierId));
        Assert.Equal("amount_below_minimum", ex.ErrorCode);
    }

    [Fact]
    public async Task CreatePledgeAsync_ClaimsRewardStock()
    {
        var campaign = await SeedActiveCampaignAsync(rewardStock: 2, minimumPledge: 10m);
        var tierId = campaign.RewardTiers.Single().Id;
        var backer = await SeedBackerAsync();

        await _sut.CreatePledgeAsync(campaign.Id, backer.Id, 10m, tierId);

        var tier = await _db.RewardTiers.FindAsync(tierId);
        Assert.Equal(1, tier!.ClaimedStock);
    }

    [Fact]
    public async Task CreatePledgeAsync_WhenRewardTierSoldOut_Throws()
    {
        var campaign = await SeedActiveCampaignAsync(rewardStock: 1, minimumPledge: 10m);
        var tierId = campaign.RewardTiers.Single().Id;
        var firstBacker = await SeedBackerAsync("first@test.dev");
        var secondBacker = await SeedBackerAsync("second@test.dev");

        await _sut.CreatePledgeAsync(campaign.Id, firstBacker.Id, 10m, tierId);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _sut.CreatePledgeAsync(campaign.Id, secondBacker.Id, 10m, tierId));
        Assert.Equal("reward_tier_sold_out", ex.ErrorCode);
    }

    [Fact]
    public async Task CreatePledgeAsync_WhenAnotherRequestClaimsLastUnitConcurrently_RetriesAndFails()
    {
        var campaign = await SeedActiveCampaignAsync(rewardStock: 1, minimumPledge: 10m);
        var tierId = campaign.RewardTiers.Single().Id;
        var backer = await SeedBackerAsync();

        // A concurrent request, using a separate DbContext against the same database,
        // claims the last unit and commits before our service's SaveChanges runs.
        await using var concurrentDb = SqliteTestDbContextFactory.CreateSecondContext(_connection);
        var concurrentTier = await concurrentDb.RewardTiers.FindAsync(tierId);
        concurrentTier!.ClaimedStock += 1;
        await concurrentDb.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _sut.CreatePledgeAsync(campaign.Id, backer.Id, 10m, tierId));

        Assert.Equal("reward_tier_sold_out", ex.ErrorCode);
        var pledgeCount = _db.Pledges.Count(p => p.CampaignId == campaign.Id);
        Assert.Equal(0, pledgeCount);
    }
}
