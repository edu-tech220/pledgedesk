using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PledgeDesk.Api.Contracts;
using PledgeDesk.Api.Data;

namespace PledgeDesk.Api.Controllers;

[ApiController]
[Route("api/pledges")]
[Authorize(Roles = "Backer")]
public class PledgesController : ControllerBase
{
    private readonly PledgeDeskDbContext _db;

    public PledgesController(PledgeDeskDbContext db)
    {
        _db = db;
    }

    [HttpGet("mine")]
    public async Task<ActionResult<PagedResult<PledgeDto>>> GetMyPledges(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken cancellationToken = default)
    {
        var backerId = User.GetUserId();

        var query = _db.Pledges
            .AsNoTracking()
            .Where(p => p.BackerId == backerId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .Select(p => new PledgeDto(
                p.Id, p.CampaignId, p.Campaign!.Title, p.Amount, p.RewardTierId,
                p.RewardTier != null ? p.RewardTier.Title : null, p.Status, p.CreatedAtUtc));

        var result = await query.ToPagedResultAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }
}
