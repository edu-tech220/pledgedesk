using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PledgeDesk.Api.Contracts;
using PledgeDesk.Api.Domain;
using PledgeDesk.Api.Tests.TestHelpers;

namespace PledgeDesk.Api.Tests.Api;

public class CampaignsEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public CampaignsEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> RegisterAndGetTokenAsync(string email, string role)
    {
        var response = await _client.PostAsJsonAsync(
            "api/auth/register", new RegisterRequest(email, "password123", email, role));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        return body!.Token;
    }

    private HttpClient AuthorizedClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task GetCampaigns_Anonymous_ReturnsOk()
    {
        var response = await _client.GetAsync("api/campaigns");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task CreateCampaign_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("api/campaigns", new CreateCampaignRequest(
            "T", "d", "Tech", 100m, DateTime.UtcNow.AddDays(10)));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateCampaign_AsBacker_ReturnsForbidden()
    {
        var token = await RegisterAndGetTokenAsync("backerrole@test.dev", "Backer");
        var client = AuthorizedClient(token);

        var response = await client.PostAsJsonAsync("api/campaigns", new CreateCampaignRequest(
            "T", "d", "Tech", 100m, DateTime.UtcNow.AddDays(10)));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task FullLifecycle_UnderfundedCampaign_SettlesToFailedAndRefunds()
    {
        var creatorToken = await RegisterAndGetTokenAsync("lifecycle-creator@test.dev", "Creator");
        var backerToken = await RegisterAndGetTokenAsync("lifecycle-backer@test.dev", "Backer");
        var creatorClient = AuthorizedClient(creatorToken);
        var backerClient = AuthorizedClient(backerToken);
        var adminToken = await _factory.CreateAdminAndGetTokenAsync("lifecycle-admin@test.dev");
        var adminClient = AuthorizedClient(adminToken);

        var createResponse = await creatorClient.PostAsJsonAsync("api/campaigns", new CreateCampaignRequest(
            "Underfunded gadget", "desc", "Tech", 1000m, DateTime.UtcNow.AddSeconds(2)));
        createResponse.EnsureSuccessStatusCode();
        var campaignId = await createResponse.Content.ReadFromJsonAsync<int>();

        var activateResponse = await creatorClient.PostAsync($"api/campaigns/{campaignId}/activate", null);
        activateResponse.EnsureSuccessStatusCode();

        var pledgeResponse = await backerClient.PostAsJsonAsync(
            $"api/campaigns/{campaignId}/pledges", new CreatePledgeRequest(100m, null));
        pledgeResponse.EnsureSuccessStatusCode();

        await Task.Delay(TimeSpan.FromSeconds(3));

        var settlementResponse = await adminClient.PostAsync("api/admin/settlement/run", null);
        settlementResponse.EnsureSuccessStatusCode();

        var detailResponse = await _client.GetAsync($"api/campaigns/{campaignId}");
        detailResponse.EnsureSuccessStatusCode();
        var detail = await detailResponse.Content.ReadFromJsonAsync<CampaignDetailDto>(JsonOptions);

        Assert.Equal(CampaignStatus.Failed, detail!.Status);

        var myPledgesResponse = await backerClient.GetAsync("api/pledges/mine");
        myPledgesResponse.EnsureSuccessStatusCode();
        var myPledgesJson = await myPledgesResponse.Content.ReadAsStringAsync();
        Assert.Contains("Refunded", myPledgesJson);
    }

    [Fact]
    public async Task AdminCampaigns_AsNonAdmin_ReturnsForbidden()
    {
        var token = await RegisterAndGetTokenAsync("notadmin@test.dev", "Creator");
        var client = AuthorizedClient(token);

        var response = await client.GetAsync("api/admin/campaigns");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
