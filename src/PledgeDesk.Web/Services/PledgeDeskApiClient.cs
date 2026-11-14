using System.Net.Http.Json;
using System.Text.Json;
using PledgeDesk.Web.Models;

namespace PledgeDesk.Web.Services;

public class PledgeDeskApiClient : IPledgeDeskApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;

    public PledgeDeskApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<AuthResponse> RegisterAsync(
        string email, string password, string displayName, string role, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/auth/register", new { Email = email, Password = password, DisplayName = displayName, Role = role },
            cancellationToken);
        return await ReadOrThrowAsync<AuthResponse>(response, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/auth/login", new { Email = email, Password = password }, cancellationToken);
        return await ReadOrThrowAsync<AuthResponse>(response, cancellationToken);
    }

    public async Task<PagedResult<CampaignSummaryDto>> GetCampaignsAsync(
        string? status, string? category, int page, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(("status", status), ("category", category), ("page", page.ToString()));
        var response = await _httpClient.GetAsync($"api/campaigns{query}", cancellationToken);
        return await ReadOrThrowAsync<PagedResult<CampaignSummaryDto>>(response, cancellationToken);
    }

    public async Task<CampaignDetailDto?> GetCampaignAsync(int id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/campaigns/{id}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        return await ReadOrThrowAsync<CampaignDetailDto>(response, cancellationToken);
    }

    public async Task<int> CreateCampaignAsync(
        string title, string description, string category, decimal goalAmount, DateTime deadlineUtc,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "api/campaigns",
            new { Title = title, Description = description, Category = category, GoalAmount = goalAmount, DeadlineUtc = deadlineUtc },
            cancellationToken);
        return await ReadOrThrowAsync<int>(response, cancellationToken);
    }

    public async Task AddRewardTierAsync(
        int campaignId, string title, string description, decimal minimumPledge, int? totalStock,
        CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/campaigns/{campaignId}/reward-tiers",
            new { Title = title, Description = description, MinimumPledge = minimumPledge, TotalStock = totalStock },
            cancellationToken);
        await ThrowIfNotSuccessAsync(response, cancellationToken);
    }

    public async Task ActivateCampaignAsync(int campaignId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"api/campaigns/{campaignId}/activate", null, cancellationToken);
        await ThrowIfNotSuccessAsync(response, cancellationToken);
    }

    public async Task CloseCampaignAsync(int campaignId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"api/campaigns/{campaignId}/close", null, cancellationToken);
        await ThrowIfNotSuccessAsync(response, cancellationToken);
    }

    public async Task CreatePledgeAsync(
        int campaignId, decimal amount, int? rewardTierId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            $"api/campaigns/{campaignId}/pledges", new { Amount = amount, RewardTierId = rewardTierId }, cancellationToken);
        await ThrowIfNotSuccessAsync(response, cancellationToken);
    }

    public async Task<PagedResult<CampaignSummaryDto>> GetMyCampaignsAsync(int page, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/campaigns/mine?page={page}", cancellationToken);
        return await ReadOrThrowAsync<PagedResult<CampaignSummaryDto>>(response, cancellationToken);
    }

    public async Task<PagedResult<PledgeDto>> GetMyPledgesAsync(int page, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/pledges/mine?page={page}", cancellationToken);
        return await ReadOrThrowAsync<PagedResult<PledgeDto>>(response, cancellationToken);
    }

    public async Task<PagedResult<PledgeDto>> GetCampaignPledgesAsync(int campaignId, int page, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/campaigns/{campaignId}/pledges?page={page}", cancellationToken);
        return await ReadOrThrowAsync<PagedResult<PledgeDto>>(response, cancellationToken);
    }

    public async Task<PagedResult<CampaignSummaryDto>> GetAdminCampaignsAsync(
        string? status, int page, CancellationToken cancellationToken = default)
    {
        var query = BuildQuery(("status", status), ("page", page.ToString()));
        var response = await _httpClient.GetAsync($"api/admin/campaigns{query}", cancellationToken);
        return await ReadOrThrowAsync<PagedResult<CampaignSummaryDto>>(response, cancellationToken);
    }

    public async Task<string> RunSettlementAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync("api/admin/settlement/run", null, cancellationToken);
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        await ThrowIfNotSuccessAsync(response, cancellationToken);
        return content;
    }

    private static string BuildQuery(params (string Key, string? Value)[] parameters)
    {
        var parts = parameters
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value!)}")
            .ToList();

        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }

    private static async Task<T> ReadOrThrowAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await ThrowIfNotSuccessAsync(response, cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        return result ?? throw new ApiException((int)response.StatusCode, "The API returned an empty response.");
    }

    private static async Task ThrowIfNotSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsResponse>(JsonOptions, cancellationToken);
        var message = problem?.Detail ?? $"The API request failed with status {(int)response.StatusCode}.";
        throw new ApiException((int)response.StatusCode, message);
    }
}
