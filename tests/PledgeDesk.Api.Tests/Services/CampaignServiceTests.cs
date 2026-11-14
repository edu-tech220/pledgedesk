using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PledgeDesk.Api.Domain;
using PledgeDesk.Api.Services;
using PledgeDesk.Api.Tests.TestHelpers;

namespace PledgeDesk.Api.Tests.Services;

public class CampaignServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly Data.PledgeDeskDbContext _db;
    private readonly FakeClock _clock = new();
    private readonly CampaignService _sut;

    public CampaignServiceTests()
    {
        (_db, _connection) = SqliteTestDbContextFactory.Create();
        _sut = new CampaignService(_db, _clock);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task<User> SeedCreatorAsync(string email = "creator@test.dev")
    {
        var user = new User { Email = email, DisplayName = "Creator", Role = UserRole.Creator };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task CreateCampaignAsync_WithValidData_CreatesDraft()
    {
        var creator = await SeedCreatorAsync();

        var campaign = await _sut.CreateCampaignAsync(
            creator.Id, "My campaign", "desc", "Tech", 500m, _clock.UtcNow.AddDays(10));

        Assert.Equal(CampaignStatus.Draft, campaign.Status);
        Assert.Equal(creator.Id, campaign.CreatorId);
    }

    [Fact]
    public async Task CreateCampaignAsync_WithPastDeadline_Throws()
    {
        var creator = await SeedCreatorAsync();

        var ex = await Assert.ThrowsAsync<DomainException>(() => _sut.CreateCampaignAsync(
            creator.Id, "My campaign", "desc", "Tech", 500m, _clock.UtcNow.AddDays(-1)));
        Assert.Equal("invalid_deadline", ex.ErrorCode);
    }

    [Fact]
    public async Task AddRewardTierAsync_WhenNotOwner_Throws()
    {
        var creator = await SeedCreatorAsync();
        var otherCreator = await SeedCreatorAsync("other@test.dev");
        var campaign = await _sut.CreateCampaignAsync(creator.Id, "T", "d", "Tech", 500m, _clock.UtcNow.AddDays(10));

        var ex = await Assert.ThrowsAsync<DomainException>(() => _sut.AddRewardTierAsync(
            campaign.Id, otherCreator.Id, "Tier", "d", 10m, null));
        Assert.Equal("not_campaign_owner", ex.ErrorCode);
    }

    [Fact]
    public async Task AddRewardTierAsync_AfterActivation_Throws()
    {
        var creator = await SeedCreatorAsync();
        var campaign = await _sut.CreateCampaignAsync(creator.Id, "T", "d", "Tech", 500m, _clock.UtcNow.AddDays(10));
        await _sut.ActivateCampaignAsync(campaign.Id, creator.Id);

        var ex = await Assert.ThrowsAsync<DomainException>(() => _sut.AddRewardTierAsync(
            campaign.Id, creator.Id, "Tier", "d", 10m, null));
        Assert.Equal("campaign_not_draft", ex.ErrorCode);
    }

    [Fact]
    public async Task ActivateCampaignAsync_TwiceInARow_SecondCallThrows()
    {
        var creator = await SeedCreatorAsync();
        var campaign = await _sut.CreateCampaignAsync(creator.Id, "T", "d", "Tech", 500m, _clock.UtcNow.AddDays(10));
        await _sut.ActivateCampaignAsync(campaign.Id, creator.Id);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _sut.ActivateCampaignAsync(campaign.Id, creator.Id));
        Assert.Equal("campaign_not_draft", ex.ErrorCode);
    }

    [Fact]
    public async Task CloseCampaignAsync_WhenFunded_MarksConfirmedPledgesCollected()
    {
        var creator = await SeedCreatorAsync();
        var backer = new User { Email = "backer@test.dev", DisplayName = "Backer", Role = UserRole.Backer };
        _db.Users.Add(backer);
        await _db.SaveChangesAsync();

        var campaign = await _sut.CreateCampaignAsync(creator.Id, "T", "d", "Tech", 100m, _clock.UtcNow.AddDays(10));
        await _sut.ActivateCampaignAsync(campaign.Id, creator.Id);

        _db.Pledges.Add(new Pledge
        {
            CampaignId = campaign.Id,
            BackerId = backer.Id,
            Amount = 150m,
            Status = PledgeStatus.Confirmed,
            CreatedAtUtc = _clock.UtcNow
        });
        await _db.SaveChangesAsync();

        campaign.Status = CampaignStatus.Funded;
        await _db.SaveChangesAsync();

        await _sut.CloseCampaignAsync(campaign.Id, creator.Id);

        var pledge = await _db.Pledges.FirstAsync(p => p.CampaignId == campaign.Id);
        Assert.Equal(PledgeStatus.Collected, pledge.Status);
    }
}
