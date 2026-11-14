using Microsoft.EntityFrameworkCore;
using PledgeDesk.Api.Domain;

namespace PledgeDesk.Api.Data;

public class PledgeDeskDbContext : DbContext
{
    public PledgeDeskDbContext(DbContextOptions<PledgeDeskDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<RewardTier> RewardTiers => Set<RewardTier>();
    public DbSet<Pledge> Pledges => Set<Pledge>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).HasMaxLength(256).IsRequired();
            entity.Property(u => u.DisplayName).HasMaxLength(128).IsRequired();
            entity.Property(u => u.Role).HasConversion<string>().HasMaxLength(32);
        });

        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.Property(c => c.Title).HasMaxLength(200).IsRequired();
            entity.Property(c => c.Category).HasMaxLength(64).IsRequired();
            entity.Property(c => c.GoalAmount).HasColumnType("decimal(18,2)");
            entity.Property(c => c.Status).HasConversion<string>().HasMaxLength(32);

            entity.HasOne(c => c.Creator)
                .WithMany(u => u.Campaigns)
                .HasForeignKey(c => c.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(c => new { c.Status, c.Category });
        });

        modelBuilder.Entity<RewardTier>(entity =>
        {
            entity.Property(r => r.Title).HasMaxLength(200).IsRequired();
            entity.Property(r => r.MinimumPledge).HasColumnType("decimal(18,2)");

            entity.HasOne(r => r.Campaign)
                .WithMany(c => c.RewardTiers)
                .HasForeignKey(r => r.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Pledge>(entity =>
        {
            entity.Property(p => p.Amount).HasColumnType("decimal(18,2)");
            entity.Property(p => p.Status).HasConversion<string>().HasMaxLength(32);

            entity.HasOne(p => p.Campaign)
                .WithMany(c => c.Pledges)
                .HasForeignKey(p => p.CampaignId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.Backer)
                .WithMany(u => u.Pledges)
                .HasForeignKey(p => p.BackerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(p => p.RewardTier)
                .WithMany(r => r.Pledges)
                .HasForeignKey(p => p.RewardTierId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(p => new { p.CampaignId, p.Status });
        });
    }
}
