using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PledgeDesk.Web.Models;
using PledgeDesk.Web.Services;

namespace PledgeDesk.Web.Pages.Creator;

[Authorize(Roles = "Creator")]
public class DashboardModel : PageModel
{
    private readonly IPledgeDeskApiClient _apiClient;

    public DashboardModel(IPledgeDeskApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public PagedResult<CampaignSummaryDto>? Campaigns { get; set; }

    public async Task OnGetAsync()
    {
        Campaigns = await _apiClient.GetMyCampaignsAsync(1);
    }
}
