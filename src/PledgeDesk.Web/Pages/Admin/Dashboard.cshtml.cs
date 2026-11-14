using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PledgeDesk.Web.Models;
using PledgeDesk.Web.Services;

namespace PledgeDesk.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public class DashboardModel : PageModel
{
    private readonly IPledgeDeskApiClient _apiClient;

    public DashboardModel(IPledgeDeskApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public PagedResult<CampaignSummaryDto>? Campaigns { get; set; }
    public string? SettlementResult { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    public async Task OnGetAsync()
    {
        Campaigns = await _apiClient.GetAdminCampaignsAsync(Status, 1);
    }

    public async Task<IActionResult> OnPostRunSettlementAsync()
    {
        SettlementResult = await _apiClient.RunSettlementAsync();
        Campaigns = await _apiClient.GetAdminCampaignsAsync(Status, 1);
        return Page();
    }
}
