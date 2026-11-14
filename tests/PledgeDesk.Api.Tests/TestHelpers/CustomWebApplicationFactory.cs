using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PledgeDesk.Api.Data;
using PledgeDesk.Api.Domain;
using PledgeDesk.Api.Services;

namespace PledgeDesk.Api.Tests.TestHelpers;

/// <summary>
/// Boots the real API pipeline (controllers, auth, middleware) against an isolated
/// in-memory SQLite database instead of SQL Server, and in the "Testing" environment
/// so Program.cs skips the startup migration/seed and the settlement BackgroundService.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public CustomWebApplicationFactory()
    {
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<PledgeDeskDbContext>));
            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<PledgeDeskDbContext>(options => options.UseSqlite(_connection));

            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PledgeDeskDbContext>();
            db.Database.EnsureCreated();
        });
    }

    /// <summary>
    /// Self-registration cannot create an Admin (see AuthService), so admin-only
    /// endpoint tests seed one directly through the DI container instead.
    /// </summary>
    public async Task<string> CreateAdminAndGetTokenAsync(string email)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PledgeDeskDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var jwtTokenService = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

        var (hash, salt) = passwordHasher.Hash("Admin#12345");
        var admin = new User
        {
            Email = email,
            DisplayName = "Test Admin",
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = UserRole.Admin,
            CreatedAtUtc = DateTime.UtcNow
        };
        db.Users.Add(admin);
        await db.SaveChangesAsync();

        return jwtTokenService.CreateToken(admin);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
