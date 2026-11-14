using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PledgeDesk.Web.Services;

namespace PledgeDesk.Web.Pages.Creator;

[Authorize(Roles = "Creator")]
public class CreateModel : PageModel
{
    private readonly IPledgeDeskApiClient _apiClient;

    public CreateModel(IPledgeDeskApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(4000)]
        public string Description { get; set; } = string.Empty;

        [Required, MaxLength(64)]
        public string Category { get; set; } = string.Empty;

        [Required, Range(0.01, double.MaxValue)]
        public decimal GoalAmount { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime DeadlineUtc { get; set; } = DateTime.UtcNow.AddDays(30);
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var id = await _apiClient.CreateCampaignAsync(
                Input.Title, Input.Description, Input.Category, Input.GoalAmount,
                DateTime.SpecifyKind(Input.DeadlineUtc, DateTimeKind.Utc));
            return RedirectToPage("/Creator/Manage", new { id });
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }
}
