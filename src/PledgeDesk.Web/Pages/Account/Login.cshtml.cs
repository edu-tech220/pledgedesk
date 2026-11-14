using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PledgeDesk.Web.Services;

namespace PledgeDesk.Web.Pages.Account;

public class LoginModel : PageModel
{
    private readonly IPledgeDeskApiClient _apiClient;

    public LoginModel(IPledgeDeskApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public class InputModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
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
            var auth = await _apiClient.LoginAsync(Input.Email, Input.Password);

            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, auth.DisplayName),
                new(ClaimTypes.Email, auth.Email),
                new(ClaimTypes.Role, auth.Role),
                new(AuthCookieAuthHandler.AccessTokenClaimType, auth.Token)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            return !string.IsNullOrEmpty(ReturnUrl) ? LocalRedirect(ReturnUrl) : RedirectToPage("/Index");
        }
        catch (ApiException ex)
        {
            ErrorMessage = ex.Message;
            return Page();
        }
    }
}
