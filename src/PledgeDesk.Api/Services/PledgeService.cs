using Microsoft.EntityFrameworkCore;
using PledgeDesk.Api.Data;
using PledgeDesk.Api.Domain;

namespace PledgeDesk.Api.Services;

/// <summary>
/// Creates pledges and, when a reward tier is selected, claims one unit of its stock
/// under optimistic concurrency: several backers can race for the last unit of a
/// limited reward, and only as many as TotalStock allows may win.
/// </summary>
public class PledgeService : IPledgeService
{
    private const int MaxConcurrencyRetries = 3;

    private readonly PledgeDeskDbContext _db;
    private readonly IClock _clock;

    public PledgeService(PledgeDeskDbContext db, IClock clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Pledge> CreatePledgeAsync(
        int campaignId,
        int backerId,
        decimal amount,
        int? rewardTierId,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            throw new DomainException("invalid_amount", "Pledge amount must be greater than zero.");
        }

        var campaign = await _db.Campaigns.FirstOrDefaultAsync(c => c.Id == campaignId, cancellationToken)
            ?? throw new DomainException("campaign_not_found", $"Campaign {campaignId} was not found.");

        CampaignStateMachine.EnsureAcceptingPledges(campaign, _clock.UtcNow);

        for (var attempt = 1; attempt <= MaxConcurrencyRetries; attempt++)
        {
            RewardTier? tier = null;

            if (rewardTierId.HasValue)
            {
                tier = await _db.RewardTiers
                    .FirstOrDefaultAsync(t => t.Id == rewardTierId.Value && t.CampaignId == campaignId, cancellationToken)
                    ?? throw new DomainException("reward_tier_not_found", $"Reward tier {rewardTierId} was not found.");

                if (amount < tier.MinimumPledge)
                {
                    throw new DomainException(
                        "amount_below_minimum",
                        $"Pledge amount must be at least {tier.MinimumPledge} for this reward tier.");
                }

                if (tier.TotalStock.HasValue && tier.ClaimedStock + 1 > tier.TotalStock.Value)
                {
                    throw new DomainException("reward_tier_sold_out", "This reward tier has no stock left.");
                }

                tier.ClaimedStock += 1;
            }

            var pledge = new Pledge
            {
                CampaignId = campaignId,
                BackerId = backerId,
                RewardTierId = rewardTierId,
                Amount = amount,
                Status = PledgeStatus.Confirmed,
                CreatedAtUtc = _clock.UtcNow
            };
            _db.Pledges.Add(pledge);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
                return pledge;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _db.Pledges.Remove(pledge);

                if (attempt == MaxConcurrencyRetries)
                {
                    break;
                }

                foreach (var entry in ex.Entries)
                {
                    await entry.ReloadAsync(cancellationToken);
                }
            }
        }

        throw new DomainException(
            "concurrency_conflict",
            "Could not process the pledge due to concurrent updates; please retry.");
    }
}
