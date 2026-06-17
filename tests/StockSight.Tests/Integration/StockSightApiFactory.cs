using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using StockSight.Core.Interfaces;
using StockSight.Infrastructure.Caching;
using StockSight.Infrastructure.Data;
using StockSight.Infrastructure.MarketData;

namespace StockSight.Tests.Integration;

public class StockSightApiFactory : WebApplicationFactory<Program>
{
    public const string TestUserIdHeader = "X-Test-UserId";
    private readonly string _databaseName = $"StockSight.Tests.{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Data:UseInMemory"] = "true",
                ["Cache:UseInMemory"] = "true",
                ["MarketData:UseMock"] = "true",
                ["Hangfire:Enabled"] = "false",
                ["StockIngestion:Enabled"] = "false",
                ["Jwt:Issuer"] = "StockSight.Tests",
                ["Jwt:Audience"] = "StockSight.Tests",
                ["Jwt:Secret"] = "stocksight-integration-tests-secret-key"
            });
        });
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<StockSightDbContext>>();
            services.RemoveAll<IConnectionMultiplexer>();
            services.RemoveAll<ICacheService>();
            services.RemoveAll<IStockDataProvider>();

            services.AddDbContext<StockSightDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
            services.AddSingleton<ICacheService, InMemoryCacheService>();
            services.AddSingleton<IStockDataProvider, MockStockDataProvider>();
            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                    options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }
}

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "TestAuth";

    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(StockSightApiFactory.TestUserIdHeader, out var userIdHeader) ||
            !Guid.TryParse(userIdHeader.ToString(), out var userId))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, "integration@stocksight.local"),
            new Claim(ClaimTypes.Name, "Integration Trader")
        };
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
