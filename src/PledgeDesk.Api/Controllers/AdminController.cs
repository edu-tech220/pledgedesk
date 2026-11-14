using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PledgeDesk.Api.Contracts;
using PledgeDesk.Api.Data;
using PledgeDesk.Api.Domain;

namespace PledgeDesk.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly PledgeDeskDbContext _db;

    public AdminController(PledgeDeskDbContext db)
    {
        _db = db;
    }

    [HttpGet("campaigns")]
    public async Task<ActionResult<PagedResult<CampaignSummaryDto>>> GetAllCampaigns(
        [FromQuery] CampaignStatus? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var query = _db.Campaigns.AsNoTracking().AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(c => c.Status == status.Value);
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
}
