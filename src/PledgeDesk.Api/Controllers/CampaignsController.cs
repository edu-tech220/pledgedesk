using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PledgeDesk.Api.Contracts;
using PledgeDesk.Api.Data;
using PledgeDesk.Api.Domain;
using PledgeDesk.Api.Services;

namespace PledgeDesk.Api.Controllers;

[ApiController]
[Route("api/campaigns")]
public class CampaignsController : ControllerBase
{
    private readonly PledgeDeskDbContext _db;
    private readonly ICampaignService _campaignService;
    private readonly IPledgeService _pledgeService;

    public CampaignsController(PledgeDeskDbContext db, ICampaignService campaignService, IPledgeService pledgeService)
    {
        _db = db;
        _campaignService = campaignService;
        _pledgeService = pledgeService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<CampaignSummaryDto>>> GetCampaigns(
        [FromQuery] CampaignStatus? status, [FromQuery] string? category,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var query = _db.Campaigns.AsNoTracking().AsQueryable();

        query = status.HasValue ? query.Where(c => c.Status == status.Value) : query.Where(c => c.Status != CampaignStatus.Draft);

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(c => c.Category == category);
        }

        var projected = query
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new CampaignSummaryDto(
                c.Id,
                c.Title,
                c.Category,
                c.GoalAmount,
                c.Pledges.Where(p => p.Status != PledgeStatus.Refunded).Sum(p => (decimal?)p.Amount) ?? 0m,
                c.DeadlineUtc,
                c.Status));

        var result = await projected.ToPagedResultAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("mine")]
    [Authorize(Roles = "Creator")]
    public async Task<ActionResult<PagedResult<CampaignSummaryDto>>> GetMyCampaigns(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var creatorId = User.GetUserId();

        var query = _db.Campaigns
            .AsNoTracking()
            .Where(c => c.CreatorId == creatorId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Select(c => new CampaignSummaryDto(
                c.Id,
                c.Title,
                c.Category,
                c.GoalAmount,
                c.Pledges.Where(p => p.Status != PledgeStatus.Refunded).Sum(p => (decimal?)p.Amount) ?? 0m,
                c.DeadlineUtc,
                c.Status));

        var result = await query.ToPagedResultAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<CampaignDetailDto>> GetCampaign(int id, CancellationToken cancellationToken)
    {
        var campaign = await _db.Campaigns
            .AsNoTracking()
            .Include(c => c.Creator)
            .Include(c => c.RewardTiers)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (campaign is null)
        {
            return NotFound();
        }

        var pledgedAmount = await _db.Pledges
            .Where(p => p.CampaignId == id && p.Status != PledgeStatus.Refunded)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var dto = new CampaignDetailDto(
            campaign.Id,
            campaign.Title,
            campaign.Description,
            campaign.Category,
            campaign.Creator!.DisplayName,
            campaign.GoalAmount,
            pledgedAmount,
            campaign.DeadlineUtc,
            campaign.Status,
            campaign.CreatedAtUtc,
            campaign.RewardTiers
                .Select(t => new RewardTierDto(
                    t.Id, t.Title, t.Description, t.MinimumPledge, t.TotalStock, t.ClaimedStock,
                    t.TotalStock.HasValue ? t.TotalStock.Value - t.ClaimedStock : null))
                .ToList());

        return Ok(dto);
    }

    [HttpPost]
    [Authorize(Roles = "Creator")]
    public async Task<ActionResult<CampaignDetailDto>> CreateCampaign(
        CreateCampaignRequest request, CancellationToken cancellationToken)
    {
        var campaign = await _campaignService.CreateCampaignAsync(
            User.GetUserId(), request.Title, request.Description, request.Category, request.GoalAmount,
            request.DeadlineUtc, cancellationToken);

        return CreatedAtAction(nameof(GetCampaign), new { id = campaign.Id }, campaign.Id);
    }

    [HttpPost("{id:int}/reward-tiers")]
    [Authorize(Roles = "Creator")]
    public async Task<ActionResult<RewardTierDto>> AddRewardTier(
        int id, CreateRewardTierRequest request, CancellationToken cancellationToken)
    {
        var tier = await _campaignService.AddRewardTierAsync(
            id, User.GetUserId(), request.Title, request.Description, request.MinimumPledge, request.TotalStock,
            cancellationToken);

        return Ok(new RewardTierDto(
            tier.Id, tier.Title, tier.Description, tier.MinimumPledge, tier.TotalStock, tier.ClaimedStock,
            tier.TotalStock));
    }

    [HttpPost("{id:int}/activate")]
    [Authorize(Roles = "Creator")]
    public async Task<IActionResult> ActivateCampaign(int id, CancellationToken cancellationToken)
    {
        await _campaignService.ActivateCampaignAsync(id, User.GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/close")]
    [Authorize(Roles = "Creator")]
    public async Task<IActionResult> CloseCampaign(int id, CancellationToken cancellationToken)
    {
        await _campaignService.CloseCampaignAsync(id, User.GetUserId(), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/pledges")]
    [Authorize(Roles = "Backer")]
    public async Task<ActionResult<PledgeDto>> CreatePledge(
        int id, CreatePledgeRequest request, CancellationToken cancellationToken)
    {
        var pledge = await _pledgeService.CreatePledgeAsync(
            id, User.GetUserId(), request.Amount, request.RewardTierId, cancellationToken);

        return Ok(new PledgeDto(pledge.Id, pledge.CampaignId, string.Empty, pledge.Amount, pledge.RewardTierId,
            null, pledge.Status, pledge.CreatedAtUtc));
    }

    [HttpGet("{id:int}/pledges")]
    [Authorize(Roles = "Creator,Admin")]
    public async Task<ActionResult<PagedResult<PledgeDto>>> GetCampaignPledges(
        int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var campaign = await _db.Campaigns.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (campaign is null)
        {
            return NotFound();
        }

        if (!User.IsInRole("Admin") && campaign.CreatorId != User.GetUserId())
        {
            return Forbid();
        }

        var query = _db.Pledges
            .AsNoTracking()
            .Where(p => p.CampaignId == id)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new PledgeDto(
                p.Id, p.CampaignId, p.Campaign!.Title, p.Amount, p.RewardTierId,
                p.RewardTier != null ? p.RewardTier.Title : null, p.Status, p.CreatedAtUtc));

        var result = await query.ToPagedResultAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }
}
