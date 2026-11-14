namespace PledgeDesk.Web.Models;

public record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public record AuthResponse(string Token, string Email, string DisplayName, string Role);

public record RewardTierDto(
    int Id, string Title, string Description, decimal MinimumPledge, int? TotalStock, int ClaimedStock,
    int? AvailableStock);

public record CampaignSummaryDto(
    int Id, string Title, string Category, decimal GoalAmount, decimal PledgedAmount, DateTime DeadlineUtc,
    string Status);

public record CampaignDetailDto(
    int Id, string Title, string Description, string Category, string CreatorDisplayName, decimal GoalAmount,
    decimal PledgedAmount, DateTime DeadlineUtc, string Status, DateTime CreatedAtUtc,
    IReadOnlyList<RewardTierDto> RewardTiers);

public record PledgeDto(
    int Id, int CampaignId, string CampaignTitle, decimal Amount, int? RewardTierId, string? RewardTierTitle,
    string Status, DateTime CreatedAtUtc);

public record ProblemDetailsResponse(string? Title, string? Detail, int? Status);
