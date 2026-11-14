using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PledgeDesk.Web.Models;
using PledgeDesk.Web.Services;

namespace PledgeDesk.Web.Pages;

public class IndexModel : PageModel
{
    private readonly IPledgeDeskApiClient _apiClient;

    public IndexModel(IPledgeDeskApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public PagedResult<CampaignSummaryDto>? Campaigns { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Category { get; set; }

    [BindProperty(SupportsGet = true, Name = "page")]
    public int PageNumber { get; set; } = 1;

    public async Task OnGetAsync()
    {
        Campaigns = await _apiClient.GetCampaignsAsync(Status, Category, PageNumber);
    }
}
