using PledgeDesk.Api.Domain;

namespace PledgeDesk.Api.Tests.Domain;

public class CampaignStateMachineTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static Campaign NewDraftCampaign(decimal goal = 1000m, DateTime? deadline = null) => new()
    {
        Id = 1,
        Status = CampaignStatus.Draft,
        GoalAmount = goal,
        DeadlineUtc = deadline ?? Now.AddDays(30)
    };

    [Fact]
    public void Activate_FromDraft_WithValidGoalAndFutureDeadline_Succeeds()
    {
        var campaign = NewDraftCampaign();

        CampaignStateMachine.Activate(campaign, Now);

        Assert.Equal(CampaignStatus.Active, campaign.Status);
        Assert.Equal(Now, campaign.ActivatedAtUtc);
    }

    [Fact]
    public void Activate_WhenNotDraft_Throws()
    {
        var campaign = NewDraftCampaign();
        campaign.Status = CampaignStatus.Active;

        var ex = Assert.Throws<DomainException>(() => CampaignStateMachine.Activate(campaign, Now));
        Assert.Equal("campaign_not_draft", ex.ErrorCode);
    }

    [Fact]
    public void Activate_WithZeroGoal_Throws()
    {
        var campaign = NewDraftCampaign(goal: 0);

        var ex = Assert.Throws<DomainException>(() => CampaignStateMachine.Activate(campaign, Now));
        Assert.Equal("invalid_goal", ex.ErrorCode);
    }

    [Fact]
    public void Activate_WithPastDeadline_Throws()
    {
        var campaign = NewDraftCampaign(deadline: Now.AddDays(-1));

        var ex = Assert.Throws<DomainException>(() => CampaignStateMachine.Activate(campaign, Now));
        Assert.Equal("invalid_deadline", ex.ErrorCode);
    }

    [Fact]
    public void EnsureAcceptingPledges_WhenActiveAndBeforeDeadline_DoesNotThrow()
    {
        var campaign = NewDraftCampaign();
        CampaignStateMachine.Activate(campaign, Now);

        var exception = Record.Exception(() => CampaignStateMachine.EnsureAcceptingPledges(campaign, Now.AddDays(1)));

        Assert.Null(exception);
    }

    [Fact]
    public void EnsureAcceptingPledges_WhenDraft_Throws()
    {
        var campaign = NewDraftCampaign();

        var ex = Assert.Throws<DomainException>(() => CampaignStateMachine.EnsureAcceptingPledges(campaign, Now));
        Assert.Equal("campaign_not_active", ex.ErrorCode);
    }

    [Fact]
    public void EnsureAcceptingPledges_AfterDeadline_Throws()
    {
        var campaign = NewDraftCampaign();
        CampaignStateMachine.Activate(campaign, Now);

        var ex = Assert.Throws<DomainException>(
            () => CampaignStateMachine.EnsureAcceptingPledges(campaign, campaign.DeadlineUtc.AddSeconds(1)));
        Assert.Equal("campaign_past_deadline", ex.ErrorCode);
    }

    [Fact]
    public void Settle_WithPledgesMeetingGoal_BecomesFunded()
    {
        var campaign = NewDraftCampaign(goal: 1000m);
        CampaignStateMachine.Activate(campaign, Now);
        var settleTime = campaign.DeadlineUtc;

        CampaignStateMachine.Settle(campaign, totalPledgedAmount: 1200m, settleTime);

        Assert.Equal(CampaignStatus.Funded, campaign.Status);
        Assert.Equal(settleTime, campaign.SettledAtUtc);
    }

    [Fact]
    public void Settle_WithPledgesBelowGoal_BecomesFailed()
    {
        var campaign = NewDraftCampaign(goal: 1000m);
        CampaignStateMachine.Activate(campaign, Now);

        CampaignStateMachine.Settle(campaign, totalPledgedAmount: 500m, campaign.DeadlineUtc);

        Assert.Equal(CampaignStatus.Failed, campaign.Status);
    }

    [Fact]
    public void Settle_BeforeDeadline_Throws()
    {
        var campaign = NewDraftCampaign();
        CampaignStateMachine.Activate(campaign, Now);

        var ex = Assert.Throws<DomainException>(
            () => CampaignStateMachine.Settle(campaign, 1000m, campaign.DeadlineUtc.AddSeconds(-1)));
        Assert.Equal("campaign_not_ready_for_settlement", ex.ErrorCode);
    }

    [Fact]
    public void Close_WhenFunded_Succeeds()
    {
        var campaign = NewDraftCampaign();
        CampaignStateMachine.Activate(campaign, Now);
        CampaignStateMachine.Settle(campaign, 2000m, campaign.DeadlineUtc);

        CampaignStateMachine.Close(campaign);

        Assert.Equal(CampaignStatus.Closed, campaign.Status);
    }

    [Fact]
    public void Close_WhenNotFunded_Throws()
    {
        var campaign = NewDraftCampaign();

        var ex = Assert.Throws<DomainException>(() => CampaignStateMachine.Close(campaign));
        Assert.Equal("campaign_not_funded", ex.ErrorCode);
    }
}
