namespace PledgeDesk.Api.Services;

public record SettlementSummary(int CampaignsFunded, int CampaignsFailed, int PledgesRefunded);

public interface ISettlementService
{
    Task<SettlementSummary> RunSettlementAsync(CancellationToken cancellationToken = default);
}
