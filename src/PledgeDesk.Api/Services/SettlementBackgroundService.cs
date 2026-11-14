namespace PledgeDesk.Api.Services;

/// <summary>
/// Periodically settles past-deadline campaigns so creators and backers don't need to
/// take any action for a campaign to resolve. Runs on a fixed interval rather than a
/// precise per-campaign timer since settlement is idempotent and cheap to re-check.
/// </summary>
public class SettlementBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SettlementBackgroundService> _logger;

    public SettlementBackgroundService(IServiceScopeFactory scopeFactory, ILogger<SettlementBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var settlementService = scope.ServiceProvider.GetRequiredService<ISettlementService>();
                var summary = await settlementService.RunSettlementAsync(stoppingToken);

                if (summary.CampaignsFunded > 0 || summary.CampaignsFailed > 0)
                {
                    _logger.LogInformation(
                        "Settlement run: {Funded} funded, {Failed} failed, {Refunded} pledges refunded.",
                        summary.CampaignsFunded, summary.CampaignsFailed, summary.PledgesRefunded);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Settlement run failed.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown.
            }
        }
    }
}
