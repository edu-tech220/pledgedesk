using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PledgeDesk.Web.Models;
using PledgeDesk.Web.Services;

namespace PledgeDesk.Web.Pages.Backer;

[Authorize(Roles = "Backer")]
public class MyPledgesModel : PageModel
{
    private readonly IPledgeDeskApiClient _apiClient;

    public MyPledgesModel(IPledgeDeskApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public PagedResult<PledgeDto>? Pledges { get; set; }

    public async Task OnGetAsync()
    {
        Pledges = await _apiClient.GetMyPledgesAsync(1);
    }
}
