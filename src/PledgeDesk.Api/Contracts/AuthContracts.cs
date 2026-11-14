using System.ComponentModel.DataAnnotations;
using PledgeDesk.Api.Domain;

namespace PledgeDesk.Api.Contracts;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8)] string Password,
    [Required, MaxLength(128)] string DisplayName,
    [Required] string Role);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public record AuthResponse(string Token, string Email, string DisplayName, UserRole Role);
