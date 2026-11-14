namespace PledgeDesk.Api.Domain;

/// <summary>
/// Draft -> Active -> (Funded | Failed) -> Closed. See CampaignStateMachine for the
/// allowed transitions; this enum only defines the possible values.
/// </summary>
public enum CampaignStatus
{
    Draft,
    Active,
    Funded,
    Failed,
    Closed
}
