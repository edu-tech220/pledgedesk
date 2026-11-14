using System.Net.Http.Headers;

namespace PledgeDesk.Web.Services;

/// <summary>
/// Attaches the backend JWT - stored as a claim on the frontend's own auth cookie
/// after login - to every outgoing request to the API microservice.
/// </summary>
public class AuthCookieAuthHandler : DelegatingHandler
{
    public const string AccessTokenClaimType = "pledgedesk_access_token";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthCookieAuthHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = _httpContextAccessor.HttpContext?.User.FindFirst(AccessTokenClaimType)?.Value;
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
