using Microsoft.EntityFrameworkCore;
using PledgeDesk.Api.Data;
using PledgeDesk.Api.Domain;

namespace PledgeDesk.Api.Services;

/// <summary>
/// Implements the all-or-nothing funding rule: once a campaign's deadline passes it is
/// Funded if confirmed pledges reached the goal, otherwise Failed with every confirmed
/// pledge automatically refunded. Idempotent by construction - only Active campaigns
/// past their deadline are picked up, so re-running after a crash or on a timer never
/// double-settles or double-refunds the same campaign.
/// </summary>
public class SettlementService : ISettlementService
{
    private readonly PledgeDeskDbContext _db;
    private readonly IClock _clock;

    public SettlementService(PledgeDeskDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<SettlementSummary> RunSettlementAsync(CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;

        var dueCampaigns = await _db.Campaigns
            .Where(c => c.Status == CampaignStatus.Active && c.DeadlineUtc <= now)
            .Include(c => c.Pledges)
            .ToListAsync(cancellationToken);

        var funded = 0;
        var failed = 0;
        var refunded = 0;

        foreach (var campaign in dueCampaigns)
        {
            var confirmedPledges = campaign.Pledges.Where(p => p.Status == PledgeStatus.Confirmed).ToList();
            var totalPledged = confirmedPledges.Sum(p => p.Amount);

            CampaignStateMachine.Settle(campaign, totalPledged, now);

            if (campaign.Status == CampaignStatus.Funded)
            {
                funded++;
            }
            else
            {
                failed++;
                foreach (var pledge in confirmedPledges)
                {
                    pledge.Status = PledgeStatus.Refunded;
                    pledge.RefundedAtUtc = now;
                    refunded++;
                }
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new SettlementSummary(funded, failed, refunded);
    }
}
