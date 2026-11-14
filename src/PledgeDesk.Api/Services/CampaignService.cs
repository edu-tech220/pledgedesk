using Microsoft.EntityFrameworkCore;
using PledgeDesk.Api.Data;
using PledgeDesk.Api.Domain;

namespace PledgeDesk.Api.Services;

public class CampaignService : ICampaignService
{
    private readonly PledgeDeskDbContext _db;
    private readonly IClock _clock;

    public CampaignService(PledgeDeskDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Campaign> CreateCampaignAsync(
        int creatorId, string title, string description, string category, decimal goalAmount, DateTime deadlineUtc,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("invalid_title", "Campaign title is required.");
        }

        if (goalAmount <= 0)
        {
            throw new DomainException("invalid_goal", "Campaign goal amount must be greater than zero.");
        }

        if (deadlineUtc <= _clock.UtcNow)
        {
            throw new DomainException("invalid_deadline", "Campaign deadline must be in the future.");
        }

        var campaign = new Campaign
        {
            CreatorId = creatorId,
            Title = title.Trim(),
            Description = description.Trim(),
            Category = category.Trim(),
            GoalAmount = goalAmount,
            DeadlineUtc = deadlineUtc,
            Status = CampaignStatus.Draft,
            CreatedAtUtc = _clock.UtcNow
        };

        _db.Campaigns.Add(campaign);
        await _db.SaveChangesAsync(cancellationToken);
        return campaign;
    }

    public async Task<RewardTier> AddRewardTierAsync(
        int campaignId, int creatorId, string title, string description, decimal minimumPledge, int? totalStock,
        CancellationToken cancellationToken = default)
    {
        var campaign = await GetOwnedCampaignAsync(campaignId, creatorId, cancellationToken);

        if (campaign.Status != CampaignStatus.Draft)
        {
            throw new DomainException(
                "campaign_not_draft",
                "Reward tiers can only be added while the campaign is in Draft status.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("invalid_title", "Reward tier title is required.");
        }

        if (minimumPledge <= 0)
        {
            throw new DomainException("invalid_minimum_pledge", "Minimum pledge must be greater than zero.");
        }

        if (totalStock.HasValue && totalStock.Value <= 0)
        {
            throw new DomainException("invalid_stock", "Total stock must be greater than zero when specified.");
        }

        var tier = new RewardTier
        {
            CampaignId = campaignId,
            Title = title.Trim(),
            Description = description.Trim(),
            MinimumPledge = minimumPledge,
            TotalStock = totalStock,
            ClaimedStock = 0
        };

        _db.RewardTiers.Add(tier);
        await _db.SaveChangesAsync(cancellationToken);
        return tier;
    }

    public async Task ActivateCampaignAsync(int campaignId, int creatorId, CancellationToken cancellationToken = default)
    {
        var campaign = await GetOwnedCampaignAsync(campaignId, creatorId, cancellationToken);
        CampaignStateMachine.Activate(campaign, _clock.UtcNow);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task CloseCampaignAsync(int campaignId, int creatorId, CancellationToken cancellationToken = default)
    {
        var campaign = await GetOwnedCampaignAsync(campaignId, creatorId, cancellationToken);
        CampaignStateMachine.Close(campaign);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Campaign> GetOwnedCampaignAsync(int campaignId, int creatorId, CancellationToken cancellationToken)
    {
        var campaign = await _db.Campaigns.FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken)
            ?? throw new DomainException("campaign_not_found", $"Campaign {campaignId} was not found.");

        if (campaign.CreatorId != creatorId)
        {
            throw new DomainException("not_campaign_owner", "You do not own this campaign.");
        }

        return campaign;
    }
}
