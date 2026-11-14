using Microsoft.EntityFrameworkCore;
using PledgeDesk.Api.Contracts;
using PledgeDesk.Api.Data;
using PledgeDesk.Api.Domain;

namespace PledgeDesk.Api.Services;

/// <summary>
/// Self-registration is limited to Backer/Creator - the Admin role can only be granted
/// by seeding the database directly, never through this endpoint.
/// </summary>
public class AuthService : IAuthService
{
    private readonly PledgeDeskDbContext _db;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IClock _clock;

    public AuthService(
        PledgeDeskDbContext db, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService, IClock clock)
    {
        _db = db;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _clock = clock;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<UserRole>(request.Role, ignoreCase: true, out var role) || role == UserRole.Admin)
        {
            throw new DomainException("invalid_role", "Role must be either 'Backer' or 'Creator'.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var alreadyExists = await _db.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (alreadyExists)
        {
            throw new DomainException("email_already_registered", "An account with this email already exists.");
        }

        var (hash, salt) = _passwordHasher.Hash(request.Password);
        var user = new User
        {
            Email = normalizedEmail,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = hash,
            PasswordSalt = salt,
            Role = role,
            CreatedAtUtc = _clock.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);

        return BuildAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            throw new DomainException("invalid_credentials", "Email or password is incorrect.");
        }

        return BuildAuthResponse(user);
    }

    private AuthResponse BuildAuthResponse(User user) =>
        new(_jwtTokenService.CreateToken(user), user.Email, user.DisplayName, user.Role);
}
