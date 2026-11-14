using PledgeDesk.Api.Domain;

namespace PledgeDesk.Api.Services;

public interface IPledgeService
{
    Task<Pledge> CreatePledgeAsync(
        int campaignId,
        int backerId,
        decimal amount,
        int? rewardTierId,
        CancellationToken cancellationToken = default);
}
