namespace PledgeDesk.Api.Domain;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public List<Campaign> Campaigns { get; set; } = new();
    public List<Pledge> Pledges { get; set; } = new();
}
