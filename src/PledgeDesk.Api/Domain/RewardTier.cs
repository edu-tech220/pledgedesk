namespace PledgeDesk.Api.Domain;

/// <summary>
/// A pledge reward with optional limited stock. <see cref="TotalStock"/> of null means
/// unlimited; otherwise <see cref="ClaimedStock"/> must never exceed it (enforced via
/// optimistic concurrency on <see cref="RowVersion"/>, see RewardTierClaimService).
/// </summary>
public class RewardTier
{
    public int Id { get; set; }
    public int CampaignId { get; set; }
    public Campaign? Campaign { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MinimumPledge { get; set; }

    public int? TotalStock { get; set; }
    public int ClaimedStock { get; set; }

    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public List<Pledge> Pledges { get; set; } = new();
}
