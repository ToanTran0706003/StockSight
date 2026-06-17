# Contributing — StockSight

> Coding standards and conventions for this project. Follow these so any AI assistant or collaborator can pick up the codebase without needing to ask questions.

---

## Git Conventions

### Branch naming
```
feature/phase1-signalr-hub
feature/phase2-candlestick-chart
fix/redis-connection-timeout
chore/add-docker-compose
docs/update-api-reference
```

### Commit message format (Conventional Commits)
```
type(scope): short description

Examples:
feat(api): add RSI indicator endpoint
feat(web): add candlestick chart component
fix(infrastructure): handle Yahoo Finance rate limit
chore(ci): add GitHub Actions build pipeline
docs(readme): add live demo link
test(core): add indicator calculator unit tests
refactor(api): extract alert logic into AlertService
```

**Types:** `feat`, `fix`, `chore`, `docs`, `test`, `refactor`, `perf`, `style`

### Commit discipline
- Commit after each TODO item is checked off
- Never commit with secrets, API keys, or connection strings
- Never commit broken code to main branch
- Use `git commit --amend` to fix typos in the last commit only

---

## C# Coding Conventions

### Naming
```csharp
// Classes, interfaces, methods, properties: PascalCase
public class StockDataIngestionService { }
public interface IStockDataProvider { }
public decimal CalculateSharpeRatio() { }
public string Symbol { get; set; }

// Private fields: _camelCase with underscore prefix
private readonly IStockDataProvider _stockDataProvider;
private readonly ILogger<StockHub> _logger;

// Local variables, parameters: camelCase
var stockTick = await _provider.GetQuoteAsync(symbol);
public async Task<StockTick> GetQuoteAsync(string symbol)

// Constants: PascalCase
public const string DefaultInterval = "1d";

// Enums: PascalCase values
public enum SignalAction { Buy, Sell, Hold }
public enum AlertDirection { Above, Below }
```

### File structure (one class per file)
```csharp
// StockSight.Core/Models/StockTick.cs
namespace StockSight.Core.Models;

public class StockTick
{
    // Properties first, then constructors, then methods
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### Dependency injection pattern
```csharp
// Always use primary constructor (C# 12 / .NET 8)
public class StocksController(
    IStockDataProvider stockDataProvider,
    ICacheService cacheService,
    ILogger<StocksController> logger) : ControllerBase
{
    // No field declarations needed — compiler generates them
}
```

### Async/await rules
- All I/O methods must be async: `Task<T>` return type
- Always use `ConfigureAwait(false)` in infrastructure/core layers
- Never use `.Result` or `.Wait()` — always `await`
- Cancellation token: pass `CancellationToken ct = default` on all async methods

```csharp
// Good
public async Task<StockTick> GetQuoteAsync(string symbol, CancellationToken ct = default)
{
    var cached = await _cache.GetAsync($"quote:{symbol}", ct).ConfigureAwait(false);
    if (cached is not null) return cached;
    // ...
}
```

### Error handling
```csharp
// Custom exceptions for domain errors (not generic Exception)
throw new StockNotFoundException(symbol);
throw new InsufficientFundsException(requiredAmount, availableCash);

// In controllers: use ProblemDetails (handled by middleware)
// DO NOT catch and rethrow generic exceptions in controllers
// The GlobalExceptionHandlerMiddleware handles it
```

### Null handling
```csharp
// Use nullable reference types (enabled in .csproj)
// Use null-coalescing and null-conditional operators
var name = user?.DisplayName ?? "Anonymous";

// Use required properties instead of constructor validation
public required string Symbol { get; set; }

// Pattern matching for null checks
if (result is not null) { ... }
```

---

## Project-specific Patterns

### Repository/Service registration
All infrastructure registrations go in `StockSight.Infrastructure/DependencyInjection.cs`:
```csharp
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IStockDataProvider, YahooFinanceProvider>();
        services.AddSingleton<ICacheService, RedisCacheService>();
        // ...
        return services;
    }
}
```

Then in `Program.cs` simply: `builder.Services.AddInfrastructure(builder.Configuration);`

### DTOs vs Domain Models
- Controllers receive/return **DTOs** (in `StockSight.API/DTOs/`)
- Services work with **domain models** (in `StockSight.Core/Models/`)
- Use `Mapster` or manual mapping — never expose domain models directly in API responses
- Naming: `CreateAlertRequest`, `AlertResponse`, `StockQuoteResponse`

### SignalR Hub pattern
```csharp
// Hub methods are verbs from the client's perspective
public async Task SubscribeToSymbol(string symbol)  // client calls this
// Server calls client via:
await Clients.Group($"stock:{symbol}").SendAsync("ReceiveTick", tick);
// Client method names are PascalCase strings matching the Blazor .On<T>() registration
```

### Background Service pattern
```csharp
public class StockDataIngestionService(
    IServiceScopeFactory scopeFactory,
    ILogger<StockDataIngestionService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var provider = scope.ServiceProvider.GetRequiredService<IStockDataProvider>();
                // ... do work
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in ingestion loop");
            }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
```

---

## Blazor Conventions

### Component naming
- Pages: `{Name}Page.razor` (e.g., `WatchlistPage.razor`)
- Shared components: descriptive noun (e.g., `ChartComponent.razor`, `SignalBadge.razor`)
- Layout: `MainLayout.razor`

### Component structure (order of sections)
```razor
@page "/watchlist"
@using StockSight.Web.Services
@inject StockHubService HubService
@inject HttpClient Http
@implements IAsyncDisposable

<!-- HTML template -->
<div>...</div>

@code {
    // 1. Parameters
    [Parameter] public string Symbol { get; set; } = string.Empty;

    // 2. Private fields
    private List<StockTick> _ticks = [];
    private bool _isLoading = true;

    // 3. Lifecycle methods
    protected override async Task OnInitializedAsync() { }
    protected override async Task OnParametersSetAsync() { }

    // 4. Event handlers
    private void HandleTickReceived(StockTick tick) { }

    // 5. Dispose
    public async ValueTask DisposeAsync() { }
}
```

### JS Interop
All JS interop functions go in `wwwroot/js/chart-interop.js`. Call from Blazor via:
```csharp
await JS.InvokeVoidAsync("chartInterop.initChart", elementId);
await JS.InvokeAsync<string>("chartInterop.getData", param);
```

---

## Testing Conventions

### Test naming
```csharp
// Pattern: MethodName_StateUnderTest_ExpectedBehavior
[Fact]
public void CalculateRsi_WithOverboughtValues_ReturnAbove70()

[Fact]
public async Task GetQuoteAsync_WithInvalidSymbol_ThrowsStockNotFoundException()

[Theory]
[InlineData("AAPL", 14, 65.4)]
[InlineData("MSFT", 14, 58.1)]
public void CalculateRsi_WithKnownData_ReturnsExpectedValue(string symbol, int period, decimal expected)
```

### Test organization
```
StockSight.Tests/
├── Unit/
│   ├── Core/
│   │   ├── IndicatorCalculatorTests.cs
│   │   ├── SignalAnalyzerTests.cs
│   │   └── BacktestEngineTests.cs
│   └── Services/
│       ├── PortfolioServiceTests.cs
│       └── AlertCheckerServiceTests.cs
└── Integration/
    └── Api/
        ├── StocksControllerTests.cs
        └── AuthControllerTests.cs
```

---

## Environment Variables

Never hardcode secrets. All config goes through:
1. `appsettings.json` — non-secret defaults
2. `appsettings.Development.json` — local dev values (gitignored)
3. Environment variables — production secrets (set in Railway)

```json
// appsettings.json (safe to commit — no values)
{
  "ConnectionStrings": {
    "Postgres": "",
    "Redis": ""
  },
  "ApiKeys": {
    "AlphaVantage": "",
    "NewsApi": "",
    "OpenAI": "",
    "SendGrid": ""
  },
  "Jwt": {
    "SecretKey": "",
    "Issuer": "",
    "Audience": "",
    "ExpiryMinutes": 60
  }
}
```

Add to `.gitignore`:
```
appsettings.Development.json
appsettings.*.json
!appsettings.json
.env
*.env
```

---

## Code Review Checklist (self-review before commit)

- [ ] No hardcoded connection strings or API keys
- [ ] All new methods are async if they do I/O
- [ ] No `.Result` or `.Wait()` calls
- [ ] New domain logic has a unit test
- [ ] TODO.md updated (task checked off)
- [ ] Commit message follows conventional commits format
- [ ] No `Console.WriteLine` left in code (use `ILogger`)
- [ ] New endpoints documented in `API_REFERENCE.md`
- [ ] New models documented in `DATABASE_SCHEMA.md`
