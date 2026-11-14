using PledgeDesk.Api.Domain;

namespace PledgeDesk.Api.Services;

public interface IJwtTokenService
{
    string CreateToken(User user);
}
