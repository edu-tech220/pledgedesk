using PledgeDesk.Api.Domain;

namespace PledgeDesk.Api.Services;

public interface ICampaignService
{
    Task<Campaign> CreateCampaignAsync(
        int creatorId, string title, string description, string category, decimal goalAmount, DateTime deadlineUtc,
        CancellationToken cancellationToken = default);

    Task<RewardTier> AddRewardTierAsync(
        int campaignId, int creatorId, string title, string description, decimal minimumPledge, int? totalStock,
        CancellationToken cancellationToken = default);

    Task ActivateCampaignAsync(int campaignId, int creatorId, CancellationToken cancellationToken = default);

    Task CloseCampaignAsync(int campaignId, int creatorId, CancellationToken cancellationToken = default);
}
