using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using PledgeDesk.Api.Contracts;
using PledgeDesk.Api.Domain;
using PledgeDesk.Api.Services;
using PledgeDesk.Api.Tests.TestHelpers;

namespace PledgeDesk.Api.Tests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly Data.PledgeDeskDbContext _db;
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        (_db, _connection) = SqliteTestDbContextFactory.Create();
        var jwtOptions = Options.Create(new JwtOptions
        {
            Issuer = "test",
            Audience = "test",
            SigningKey = "unit-test-signing-key-at-least-32-bytes-long",
            AccessTokenMinutes = 60
        });
        _sut = new AuthService(_db, new Pbkdf2PasswordHasher(), new JwtTokenService(jwtOptions), new FakeClock());
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task RegisterAsync_WithBackerRole_Succeeds()
    {
        var response = await _sut.RegisterAsync(new RegisterRequest("backer@test.dev", "password123", "Backer One", "Backer"));

        Assert.Equal(UserRole.Backer, response.Role);
        Assert.False(string.IsNullOrEmpty(response.Token));
    }

    [Fact]
    public async Task RegisterAsync_WithAdminRole_Throws()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(() => _sut.RegisterAsync(
            new RegisterRequest("hacker@test.dev", "password123", "Hacker", "Admin")));
        Assert.Equal("invalid_role", ex.ErrorCode);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_Throws()
    {
        await _sut.RegisterAsync(new RegisterRequest("dup@test.dev", "password123", "First", "Backer"));

        var ex = await Assert.ThrowsAsync<DomainException>(() => _sut.RegisterAsync(
            new RegisterRequest("dup@test.dev", "password456", "Second", "Creator")));
        Assert.Equal("email_already_registered", ex.ErrorCode);
    }

    [Fact]
    public async Task LoginAsync_WithCorrectPassword_Succeeds()
    {
        await _sut.RegisterAsync(new RegisterRequest("login@test.dev", "password123", "Login User", "Backer"));

        var response = await _sut.LoginAsync(new LoginRequest("login@test.dev", "password123"));

        Assert.Equal("login@test.dev", response.Email);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_Throws()
    {
        await _sut.RegisterAsync(new RegisterRequest("wrongpw@test.dev", "password123", "User", "Backer"));

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _sut.LoginAsync(new LoginRequest("wrongpw@test.dev", "not-the-password")));
        Assert.Equal("invalid_credentials", ex.ErrorCode);
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_Throws()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(
            () => _sut.LoginAsync(new LoginRequest("ghost@test.dev", "password123")));
        Assert.Equal("invalid_credentials", ex.ErrorCode);
    }
}
