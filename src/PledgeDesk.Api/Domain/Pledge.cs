namespace PledgeDesk.Api.Domain;

public class Pledge
{
    public int Id { get; set; }
    public int CampaignId { get; set; }
    public Campaign? Campaign { get; set; }

    public int BackerId { get; set; }
    public User? Backer { get; set; }

    public int? RewardTierId { get; set; }
    public RewardTier? RewardTier { get; set; }

    public decimal Amount { get; set; }
    public PledgeStatus Status { get; set; } = PledgeStatus.Confirmed;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? RefundedAtUtc { get; set; }

    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
