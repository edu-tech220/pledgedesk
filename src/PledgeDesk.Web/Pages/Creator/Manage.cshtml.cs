using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PledgeDesk.Web.Models;
using PledgeDesk.Web.Services;

namespace PledgeDesk.Web.Pages.Creator;

[Authorize(Roles = "Creator")]
public class ManageModel : PageModel
{
    private readonly IPledgeDeskApiClient _apiClient;

    public ManageModel(IPledgeDeskApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public CampaignDetailDto? Campaign { get; set; }
    public PagedResult<PledgeDto>? Pledges { get; set; }
    public string? ErrorMessage { get; set; }

    [BindProperty]
    public RewardTierInputModel RewardTier { get; set; } = new();

    public class RewardTierInputModel
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Required, Range(0.01, double.MaxValue)]
        public decimal MinimumPledge { get; set; }

        [Range(1, int.MaxValue)]
        public int? TotalStock { get; set; }
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Campaign = await _apiClient.GetCampaignAsync(id);
        if (Campaign is null)
        {
            return NotFound();
        }

        if (Campaign.Status != "Draft")
        {
            try
            {
                Pledges = await _apiClient.GetCampaignPledgesAsync(id, 1);
            }
            catch (ApiException)
            {
                Pledges = null;
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAddRewardTierAsync(int id)
    {
        if (!ModelState.IsValid)
        {
            Campaign = await _apiClient.GetCampaignAsync(id);
            return Page();
        }

        try
        {
            await _apiClient.AddRewardTierAsync(
                id, RewardTier.Title, RewardTier.Description, RewardTier.MinimumPledge, RewardTier.TotalStock);
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostActivateAsync(int id)
    {
        try
        {
            await _apiClient.ActivateCampaignAsync(id);
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
            Campaign = await _apiClient.GetCampaignAsync(id);
            return Page();
        }

        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCloseAsync(int id)
    {
        try
        {
            await _apiClient.CloseCampaignAsync(id);
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
            Campaign = await _apiClient.GetCampaignAsync(id);
            return Page();
        }

        return RedirectToPage(new { id });
    }
}
