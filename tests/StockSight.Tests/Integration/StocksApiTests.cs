using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace StockSight.Tests.Integration;

public class StocksApiTests : IClassFixture<StockSightApiFactory>
{
    private readonly HttpClient _client;

    public StocksApiTests(StockSightApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Health_ReturnsStatusAndChecks()
    {
        var response = await _client.GetAsync("/health");

        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(json);
        Assert.True(json!.ContainsKey("status"));
        Assert.True(json.ContainsKey("checks"));
    }

    [Fact]
    public async Task Quote_ReturnsMockMarketData()
    {
        var response = await _client.GetAsync("/api/stocks/AAPL/quote");

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"symbol\":\"AAPL\"", body);
        Assert.Contains("\"price\":", body);
    }

    [Fact]
    public async Task Search_RequiresQuery()
    {
        var response = await _client.GetAsync("/api/stocks/search");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
