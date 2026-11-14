using Microsoft.AspNetCore.Mvc;
using PledgeDesk.Api.Domain;

namespace PledgeDesk.Api.Middleware;

/// <summary>
/// Converts DomainException into an RFC 7807 ProblemDetails response so controllers
/// stay free of try/catch noise. "Not found" error codes map to 404, ownership/role
/// violations to 403, everything else (invalid state, exhausted stock, etc) to 409 or
/// 400 depending on whether it is a conflict with existing state.
/// </summary>
public class DomainExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DomainExceptionMiddleware> _logger;

    private static readonly HashSet<string> NotFoundCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "campaign_not_found", "reward_tier_not_found"
    };

    private static readonly HashSet<string> ForbiddenCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "not_campaign_owner"
    };

    private static readonly HashSet<string> ConflictCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "reward_tier_sold_out", "concurrency_conflict", "campaign_not_draft", "campaign_not_active",
        "campaign_not_funded", "campaign_not_ready_for_settlement", "campaign_past_deadline",
        "email_already_registered"
    };

    public DomainExceptionMiddleware(RequestDelegate next, ILogger<DomainExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            _logger.LogInformation(ex, "Domain rule rejected request: {ErrorCode}", ex.ErrorCode);

            var statusCode = ex.ErrorCode switch
            {
                _ when NotFoundCodes.Contains(ex.ErrorCode) => StatusCodes.Status404NotFound,
                _ when ForbiddenCodes.Contains(ex.ErrorCode) => StatusCodes.Status403Forbidden,
                _ when ConflictCodes.Contains(ex.ErrorCode) => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };

            var problemDetails = new ProblemDetails
            {
                Title = ex.ErrorCode,
                Detail = ex.Message,
                Status = statusCode
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problemDetails);
        }
    }
}
