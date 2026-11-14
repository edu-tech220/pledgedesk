using System.ComponentModel.DataAnnotations;
using PledgeDesk.Api.Domain;

namespace PledgeDesk.Api.Contracts;

public record CreateCampaignRequest(
    [Required, MaxLength(200)] string Title,
    [MaxLength(4000)] string Description,
    [Required, MaxLength(64)] string Category,
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")] decimal GoalAmount,
    [Required] DateTime DeadlineUtc);

public record CreateRewardTierRequest(
    [Required, MaxLength(200)] string Title,
    [MaxLength(2000)] string Description,
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")] decimal MinimumPledge,
    [Range(1, int.MaxValue)] int? TotalStock);

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
