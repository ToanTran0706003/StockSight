using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace StockSight.Tests.Integration;

public class AuthApiTests : IClassFixture<StockSightApiFactory>
{
    private readonly HttpClient _client;

    public AuthApiTests(StockSightApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task RegisterLoginAndWatchlistFlow_WorksEndToEnd()
    {
        var email = $"integration-{Guid.NewGuid():N}@stocksight.local";

        var register = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password = "demo123",
            displayName = "Integration Trader"
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password = "demo123"
        });
        login.EnsureSuccessStatusCode();

        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth?.AccessToken);

        _client.DefaultRequestHeaders.Add(StockSightApiFactory.TestUserIdHeader, auth!.UserId.ToString());

        var watchlist = await _client.GetAsync("/api/watchlist");
        watchlist.EnsureSuccessStatusCode();

        var add = await _client.PostAsync("/api/watchlist/NVDA", null);
        Assert.Equal(HttpStatusCode.Created, add.StatusCode);
    }

    private record AuthResponse(string AccessToken, int ExpiresIn, Guid UserId, string Email, string DisplayName);
}
