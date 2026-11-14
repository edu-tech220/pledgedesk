using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using PledgeDesk.Api.Contracts;
using PledgeDesk.Api.Tests.TestHelpers;

namespace PledgeDesk.Api.Tests.Api;

public class AuthEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public AuthEndpointsTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_NewBacker_ReturnsToken()
    {
        var response = await _client.PostAsJsonAsync("api/auth/register", new RegisterRequest(
            "newbacker@test.dev", "password123", "New Backer", "Backer"));

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        Assert.False(string.IsNullOrEmpty(body!.Token));
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        var request = new RegisterRequest("dupe@test.dev", "password123", "User", "Backer");
        await _client.PostAsJsonAsync("api/auth/register", request);

        var response = await _client.PostAsJsonAsync("api/auth/register", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithAdminRole_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("api/auth/register", new RegisterRequest(
            "wannabeadmin@test.dev", "password123", "User", "Admin"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
    {
        await _client.PostAsJsonAsync("api/auth/register", new RegisterRequest(
            "loginflow@test.dev", "password123", "User", "Backer"));

        var response = await _client.PostAsJsonAsync(
            "api/auth/login", new LoginRequest("loginflow@test.dev", "password123"));

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsBadRequest()
    {
        await _client.PostAsJsonAsync("api/auth/register", new RegisterRequest(
            "badpw@test.dev", "password123", "User", "Backer"));

        var response = await _client.PostAsJsonAsync(
            "api/auth/login", new LoginRequest("badpw@test.dev", "wrong-password"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
