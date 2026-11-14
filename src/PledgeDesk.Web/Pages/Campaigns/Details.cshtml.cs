using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PledgeDesk.Web.Models;
using PledgeDesk.Web.Services;

namespace PledgeDesk.Web.Pages.Campaigns;

public class DetailsModel : PageModel
{
    private readonly IPledgeDeskApiClient _apiClient;

    public DetailsModel(IPledgeDeskApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public CampaignDetailDto? Campaign { get; set; }
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    [BindProperty]
    public PledgeInputModel Input { get; set; } = new();

    public class PledgeInputModel
    {
        [Required, Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        public int? RewardTierId { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Campaign = await _apiClient.GetCampaignAsync(id);
        return Campaign is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        Campaign = await _apiClient.GetCampaignAsync(id);
        if (Campaign is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            await _apiClient.CreatePledgeAsync(id, Input.Amount, Input.RewardTierId);
            SuccessMessage = "Thanks for backing this campaign!";
            Campaign = await _apiClient.GetCampaignAsync(id);
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }

        return Page();
    }
}
