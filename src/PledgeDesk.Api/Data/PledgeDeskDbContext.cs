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

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AssignSqliteRowVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AssignSqliteRowVersions();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// SQL Server auto-generates the rowversion column on every insert/update; SQLite
    /// (used in tests) has no equivalent, so stand in with a random value on each save.
    /// The EF concurrency check still compares against the ORIGINAL value captured at
    /// load time, so a stale write is detected exactly like it would be against SQL
    /// Server's native rowversion.
    /// </summary>
    private void AssignSqliteRowVersions()
    {
        if (Database.ProviderName != "Microsoft.EntityFrameworkCore.Sqlite")
        {
            return;
        }

        foreach (var entry in ChangeTracker.Entries().Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            var property = entry.Metadata.FindProperty("RowVersion");
            if (property is not null && property.IsConcurrencyToken)
            {
                entry.Property("RowVersion").CurrentValue = Guid.NewGuid().ToByteArray();
            }
        }
    }

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
            entity.Property(c => c.GoalAmount).HasConversion<MoneyConverter>();
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
            entity.Property(r => r.MinimumPledge).HasConversion<MoneyConverter>();

            entity.HasOne(r => r.Campaign)
                .WithMany(c => c.RewardTiers)
                .HasForeignKey(r => r.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Pledge>(entity =>
        {
            entity.Property(p => p.Amount).HasConversion<MoneyConverter>();
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

        if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            // SQL Server auto-generates rowversion as a store-computed column; SQLite has
            // no equivalent, so here it is a plain column that AssignSqliteRowVersions
            // populates before every save (see SaveChangesAsync override below).
            modelBuilder.Entity<Campaign>().Property(c => c.RowVersion).ValueGeneratedNever();
            modelBuilder.Entity<RewardTier>().Property(r => r.RowVersion).ValueGeneratedNever();
            modelBuilder.Entity<Pledge>().Property(p => p.RowVersion).ValueGeneratedNever();
        }
    }
}
