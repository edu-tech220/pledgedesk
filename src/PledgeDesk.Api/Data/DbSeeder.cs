using Microsoft.EntityFrameworkCore;
using PledgeDesk.Api.Domain;
using PledgeDesk.Api.Services;

namespace PledgeDesk.Api.Data;

/// <summary>
/// Creates the demo admin account on startup so the admin-only endpoints are
/// reachable without a manual database step. Credentials are documented in the
/// README - this is a demo seed, not a production secret.
/// </summary>
public static class DbSeeder
{
    private const string AdminEmail = "admin@pledgedesk.dev";
    private const string AdminPassword = "Admin#12345";

    public static async Task SeedAsync(PledgeDeskDbContext db, IPasswordHasher passwordHasher, IClock clock)
    {
        var adminExists = await db.Users.AnyAsync(u => u.Role == UserRole.Admin);
        if (adminExists)
        {
            return;
        }

        var (hash, salt) = passwordHasher.Hash(AdminPassword);
        db.Users.Add(new User
        {
            Email = AdminEmail,
            DisplayName = "PledgeDesk Admin",
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = UserRole.Admin,
            CreatedAtUtc = clock.UtcNow
        });

        await db.SaveChangesAsync();
    }
}
