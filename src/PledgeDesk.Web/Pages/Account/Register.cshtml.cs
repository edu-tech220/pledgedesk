using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PledgeDesk.Web.Services;
using System.Security.Claims;

namespace PledgeDesk.Web.Pages.Account;

public class RegisterModel : PageModel
{
    private readonly IPledgeDeskApiClient _apiClient;

    public RegisterModel(IPledgeDeskApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(8)]
        public string Password { get; set; } = string.Empty;

        [Required, MaxLength(128)]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "Backer";
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
            var auth = await _apiClient.RegisterAsync(Input.Email, Input.Password, Input.DisplayName, Input.Role);
            await SignInAsync(auth);
            return RedirectToPage("/Index");
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }

    private async Task SignInAsync(Models.AuthResponse auth)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, auth.DisplayName),
            new(ClaimTypes.Email, auth.Email),
            new(ClaimTypes.Role, auth.Role),
            new(AuthCookieAuthHandler.AccessTokenClaimType, auth.Token)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }
}
