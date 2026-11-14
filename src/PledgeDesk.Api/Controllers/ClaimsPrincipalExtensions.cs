using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PledgeDesk.Api.Controllers;

public static class ClaimsPrincipalExtensions
{
    public static int GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? throw new InvalidOperationException("Token is missing the subject claim.");
        return int.Parse(value);
    }
}
