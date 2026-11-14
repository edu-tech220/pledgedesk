using System.ComponentModel.DataAnnotations;
using PledgeDesk.Api.Domain;

namespace PledgeDesk.Api.Contracts;

public record RegisterRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required, MinLength(8)] string Password,
    [property: Required, MaxLength(128)] string DisplayName,
    [property: Required] string Role);

public record LoginRequest(
    [property: Required, EmailAddress] string Email,
    [property: Required] string Password);

public record AuthResponse(string Token, string Email, string DisplayName, UserRole Role);
