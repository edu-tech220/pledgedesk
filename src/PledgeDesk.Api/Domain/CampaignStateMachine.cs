namespace PledgeDesk.Api.Domain;

/// <summary>
/// Centralizes the Campaign lifecycle so every transition is validated in one place:
///   Draft -> Active -> Funded -> Closed
///                    -> Failed
/// Active is reached only from Draft; Funded/Failed only from Active (by the
/// settlement worker once the deadline has passed); Closed only from Funded.
/// </summary>
public static class CampaignStateMachine
{
    public static void Activate(Campaign campaign, DateTime nowUtc)
    {
        if (campaign.Status != CampaignStatus.Draft)
        {
            throw new DomainException(
                "campaign_not_draft",
                $"Campaign {campaign.Id} cannot be activated from status {campaign.Status}.");
        }

        if (campaign.GoalAmount <= 0)
        {
            throw new DomainException("invalid_goal", "Campaign goal amount must be greater than zero.");
        }

        if (campaign.DeadlineUtc <= nowUtc)
        {
            throw new DomainException("invalid_deadline", "Campaign deadline must be in the future.");
        }

        campaign.Status = CampaignStatus.Active;
        campaign.ActivatedAtUtc = nowUtc;
    }

    public static void EnsureAcceptingPledges(Campaign campaign, DateTime nowUtc)
    {
        if (campaign.Status != CampaignStatus.Active)
        {
            throw new DomainException(
                "campaign_not_active",
                $"Campaign {campaign.Id} is not accepting pledges (status: {campaign.Status}).");
        }

        if (campaign.DeadlineUtc <= nowUtc)
        {
            throw new DomainException("campaign_past_deadline", $"Campaign {campaign.Id} is past its deadline.");
        }
    }

    public static bool IsReadyForSettlement(Campaign campaign, DateTime nowUtc) =>
        campaign.Status == CampaignStatus.Active && campaign.DeadlineUtc <= nowUtc;

    public static void Settle(Campaign campaign, decimal totalPledgedAmount, DateTime nowUtc)
    {
        if (!IsReadyForSettlement(campaign, nowUtc))
        {
            throw new DomainException(
                "campaign_not_ready_for_settlement",
                $"Campaign {campaign.Id} is not ready for settlement.");
        }

        campaign.Status = totalPledgedAmount >= campaign.GoalAmount
            ? CampaignStatus.Funded
            : CampaignStatus.Failed;
        campaign.SettledAtUtc = nowUtc;
    }

    public static void Close(Campaign campaign)
    {
        if (campaign.Status != CampaignStatus.Funded)
        {
            throw new DomainException(
                "campaign_not_funded",
                $"Campaign {campaign.Id} cannot be closed from status {campaign.Status}.");
        }

        campaign.Status = CampaignStatus.Closed;
    }
}
