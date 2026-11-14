using System.ComponentModel.DataAnnotations;
using PledgeDesk.Api.Domain;

namespace PledgeDesk.Api.Contracts;

public record CreateCampaignRequest(
    [property: Required, MaxLength(200)] string Title,
    [property: MaxLength(4000)] string Description,
    [property: Required, MaxLength(64)] string Category,
    [property: Range(typeof(decimal), "0.01", "79228162514264337593543950335")] decimal GoalAmount,
    [property: Required] DateTime DeadlineUtc);

public record CreateRewardTierRequest(
    [property: Required, MaxLength(200)] string Title,
    [property: MaxLength(2000)] string Description,
    [property: Range(typeof(decimal), "0.01", "79228162514264337593543950335")] decimal MinimumPledge,
    [property: Range(1, int.MaxValue)] int? TotalStock);

public record RewardTierDto(
    int Id,
    string Title,
    string Description,
    decimal MinimumPledge,
    int? TotalStock,
    int ClaimedStock,
    int? AvailableStock);

public record CampaignSummaryDto(
    int Id,
    string Title,
    string Category,
    decimal GoalAmount,
    decimal PledgedAmount,
    DateTime DeadlineUtc,
    CampaignStatus Status);

public record CampaignDetailDto(
    int Id,
    string Title,
    string Description,
    string Category,
    string CreatorDisplayName,
    decimal GoalAmount,
    decimal PledgedAmount,
    DateTime DeadlineUtc,
    CampaignStatus Status,
    DateTime CreatedAtUtc,
    IReadOnlyList<RewardTierDto> RewardTiers);
