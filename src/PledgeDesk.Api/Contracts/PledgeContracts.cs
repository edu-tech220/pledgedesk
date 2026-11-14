using System.ComponentModel.DataAnnotations;
using PledgeDesk.Api.Domain;

namespace PledgeDesk.Api.Contracts;

public record CreatePledgeRequest(
    [property: Range(typeof(decimal), "0.01", "79228162514264337593543950335")] decimal Amount,
    int? RewardTierId);

public record PledgeDto(
    int Id,
    int CampaignId,
    string CampaignTitle,
    decimal Amount,
    int? RewardTierId,
    string? RewardTierTitle,
    PledgeStatus Status,
    DateTime CreatedAtUtc);
