namespace PledgeDesk.Api.Domain;

public class Campaign
{
    public int Id { get; set; }
    public int CreatorId { get; set; }
    public User? Creator { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    public decimal GoalAmount { get; set; }
    public DateTime DeadlineUtc { get; set; }
    public CampaignStatus Status { get; set; } = CampaignStatus.Draft;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ActivatedAtUtc { get; set; }
    public DateTime? SettledAtUtc { get; set; }

    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public List<RewardTier> RewardTiers { get; set; } = new();
    public List<Pledge> Pledges { get; set; } = new();
}
