using PledgeDesk.Web.Models;

namespace PledgeDesk.Web.Services;

public interface IPledgeDeskApiClient
{
    Task<AuthResponse> RegisterAsync(string email, string password, string displayName, string role, CancellationToken cancellationToken = default);
    Task<AuthResponse> LoginAsync(string email, string password, CancellationToken cancellationToken = default);

    Task<PagedResult<CampaignSummaryDto>> GetCampaignsAsync(string? status, string? category, int page, CancellationToken cancellationToken = default);
    Task<CampaignDetailDto?> GetCampaignAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateCampaignAsync(string title, string description, string category, decimal goalAmount, DateTime deadlineUtc, CancellationToken cancellationToken = default);
    Task AddRewardTierAsync(int campaignId, string title, string description, decimal minimumPledge, int? totalStock, CancellationToken cancellationToken = default);
    Task ActivateCampaignAsync(int campaignId, CancellationToken cancellationToken = default);
    Task CloseCampaignAsync(int campaignId, CancellationToken cancellationToken = default);
    Task CreatePledgeAsync(int campaignId, decimal amount, int? rewardTierId, CancellationToken cancellationToken = default);

    Task<PagedResult<CampaignSummaryDto>> GetMyCampaignsAsync(int page, CancellationToken cancellationToken = default);
    Task<PagedResult<PledgeDto>> GetMyPledgesAsync(int page, CancellationToken cancellationToken = default);
    Task<PagedResult<PledgeDto>> GetCampaignPledgesAsync(int campaignId, int page, CancellationToken cancellationToken = default);

    Task<PagedResult<CampaignSummaryDto>> GetAdminCampaignsAsync(string? status, int page, CancellationToken cancellationToken = default);
    Task<string> RunSettlementAsync(CancellationToken cancellationToken = default);
}
