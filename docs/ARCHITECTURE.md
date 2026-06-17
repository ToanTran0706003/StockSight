# Architecture — StockSight

## 1. High-Level Overview

StockSight follows **Clean Architecture** (also known as Onion Architecture). The dependency rule is strict: outer layers depend on inner layers, never the reverse.

```
┌─────────────────────────────────────────────────────────┐
│                    Presentation Layer                    │
│         StockSight.Web (Blazor WASM)                    │
│         StockSight.API (ASP.NET Core controllers)       │
├─────────────────────────────────────────────────────────┤
│                  Infrastructure Layer                    │
│         StockSight.Infrastructure                        │
│         (DB, Redis, external APIs, email)               │
├─────────────────────────────────────────────────────────┤
│                    Core Layer                           │
│         StockSight.Core                                 │
│         (models, interfaces, business logic)            │
└─────────────────────────────────────────────────────────┘
```

**Dependency rule:**
- `StockSight.Web` → `StockSight.API` (via HTTP + SignalR)
- `StockSight.API` → `StockSight.Core` + `StockSight.Infrastructure`
- `StockSight.Infrastructure` → `StockSight.Core`
- `StockSight.Core` → nothing (no external dependencies)

---

## 2. Data Flow

### Real-time tick flow (every 5 seconds)

```
Yahoo Finance API
      │
      ▼
StockDataIngestionService (BackgroundService)
      │  fetches quotes for all watched symbols
      ▼
RedisCacheService.PublishAsync("tick:{SYMBOL}", tickData)
      │
      ├──► Redis pub/sub channel
      │         │
      │         ▼
      │    AlertCheckerService (subscribed)
      │         │  checks if any user alert triggered
      │         ▼
      │    SignalR notification → client browser
      │
      └──► StockHub.BroadcastTickAsync(symbol, tick)
                │
                ▼
           SignalR groups (one group per symbol)
                │
                ▼
           All subscribed Blazor clients → UI update
```

### User request flow (REST)

```
Blazor Component
      │  HttpClient.GetAsync("/api/stocks/{symbol}/quote")
      ▼
StocksController
      │
      ├── Check Redis cache first (ICacheService.GetAsync)
      │       │ HIT → return cached value (fast path)
      │       │ MISS → continue
      ▼
IStockDataProvider.GetQuoteAsync(symbol)
      │
      ▼
YahooFinanceProvider (HTTP call to Yahoo)
      │
      ▼
StocksController → cache result in Redis (TTL: 10s)
      │
      ▼
Return JSON to Blazor component
```

---

## 3. Project Structure Detail

### StockSight.Core (no dependencies)
```
StockSight.Core/
├── Models/
│   ├── StockTick.cs
│   ├── OhlcvBar.cs
│   ├── StockInfo.cs
│   ├── User.cs
│   ├── WatchlistItem.cs
│   ├── Portfolio.cs
│   ├── PortfolioPosition.cs
│   ├── PriceAlert.cs
│   └── TradeSignal.cs
├── Interfaces/
│   ├── IStockDataProvider.cs
│   ├── ISignalEngine.cs
│   ├── ICacheService.cs
│   ├── IAlertService.cs
│   ├── IPortfolioService.cs
│   └── INewsService.cs
├── Indicators/
│   └── IndicatorCalculator.cs
├── Signals/
│   └── RuleBasedSignalAnalyzer.cs
├── Backtesting/
│   ├── BacktestEngine.cs
│   ├── BacktestResult.cs
│   ├── IBacktestStrategy.cs
│   └── Strategies/
│       ├── SmaCrossoverStrategy.cs
│       ├── RsiReversalStrategy.cs
│       └── MacdStrategy.cs
└── Exceptions/
    ├── StockNotFoundException.cs
    └── InsufficientFundsException.cs
```

### StockSight.Infrastructure (depends on Core)
```
StockSight.Infrastructure/
├── DataProviders/
│   ├── YahooFinanceProvider.cs
│   └── AlphaVantageProvider.cs
├── AI/
│   ├── SignalEngine.cs
│   └── NewsSentimentAnalyzer.cs
├── Cache/
│   └── RedisCacheService.cs
├── Persistence/
│   ├── StockSightDbContext.cs
│   └── Configurations/
│       ├── UserConfiguration.cs
│       ├── PortfolioConfiguration.cs
│       └── PriceAlertConfiguration.cs
├── Migrations/
│   └── [auto-generated]
├── Email/
│   └── SendGridEmailService.cs
└── DependencyInjection.cs    ← registers all services
```

### StockSight.API (depends on Core + Infrastructure)
```
StockSight.API/
├── Controllers/
│   ├── StocksController.cs
│   ├── AuthController.cs
│   ├── PortfolioController.cs
│   ├── AlertController.cs
│   ├── NewsController.cs
│   ├── BacktestController.cs
│   └── HealthController.cs
├── Hubs/
│   └── StockHub.cs
├── BackgroundServices/
│   ├── StockDataIngestionService.cs
│   └── AlertCheckerService.cs
├── Middleware/
│   └── GlobalExceptionHandlerMiddleware.cs
├── DTOs/
│   ├── Requests/
│   └── Responses/
├── appsettings.json
├── appsettings.Development.json
└── Program.cs
```

### StockSight.Web (Blazor WASM, depends only on HTTP/SignalR)
```
StockSight.Web/
├── Pages/
│   ├── Index.razor          → redirect to /watchlist
│   ├── WatchlistPage.razor
│   ├── StockDetailPage.razor
│   ├── PortfolioPage.razor
│   ├── BacktestPage.razor
│   ├── AlertsPage.razor
│   ├── LoginPage.razor
│   └── RegisterPage.razor
├── Shared/
│   ├── MainLayout.razor
│   ├── NavSidebar.razor
│   ├── TopBar.razor
│   └── Components/
│       ├── ChartComponent.razor
│       ├── SignalBadge.razor
│       ├── NewsPanel.razor
│       ├── PriceTickerBadge.razor
│       └── LoadingSpinner.razor
├── Services/
│   ├── StockHubService.cs
│   ├── ApiClient.cs
│   └── AuthStateProvider.cs
├── wwwroot/
│   ├── index.html
│   └── js/
│       └── chart-interop.js
└── Program.cs
```

---

## 4. SignalR Design

### Hub Groups
Each stock symbol has its own SignalR group: `stock:{SYMBOL}` (e.g., `stock:AAPL`).

When a client opens the stock detail page for AAPL, Blazor calls:
```csharp
await hubConnection.InvokeAsync("SubscribeToSymbol", "AAPL");
```

The hub adds the connection to the group:
```csharp
await Groups.AddToGroupAsync(Context.ConnectionId, $"stock:{symbol}");
```

When a new tick arrives, only clients in that group receive it:
```csharp
await Clients.Group($"stock:{symbol}").SendAsync("ReceiveTick", tick);
```

### Reconnection Strategy
Blazor SignalR client uses automatic reconnect with exponential backoff:
```csharp
new HubConnectionBuilder()
    .WithUrl("/hubs/stock")
    .WithAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .Build();
```

---

## 5. Caching Strategy

| Data | Cache Key | TTL | Reason |
|---|---|---|---|
| Current quote | `quote:{SYMBOL}` | 10 seconds | Updates frequently |
| OHLCV daily | `ohlcv:{SYMBOL}:1d` | 1 hour | Historical data stable |
| OHLCV intraday | `ohlcv:{SYMBOL}:5m` | 5 minutes | More volatile |
| Stock info | `info:{SYMBOL}` | 24 hours | Rarely changes |
| AI signal | `signal:{SYMBOL}` | 15 minutes | Expensive to compute |
| News sentiment | `news:{SYMBOL}` | 1 hour | Articles don't change |
| Indicators | `rsi:{SYMBOL}:{period}` | 5 minutes | Derived from price |

---

## 6. Authentication Design

- **JWT Bearer tokens** stored in Blazor memory state (not localStorage, not cookies)
- Token expiry: 1 hour access token, 7-day refresh token
- Refresh token stored in httpOnly cookie (server sets, client can't read)
- `AuthStateProvider` extends Blazor's built-in `AuthenticationStateProvider`
- All portfolio/alert/watchlist endpoints require `[Authorize]`
- Stock data endpoints are public (no auth needed to view prices)

---

## 7. Database Design Summary

See [DATABASE_SCHEMA.md](./DATABASE_SCHEMA.md) for full schema.

Key relationships:
- `User` has many `WatchlistItems` (many symbols)
- `User` has many `Portfolios`
- `Portfolio` has many `PortfolioPositions`
- `User` has many `PriceAlerts`
- `Portfolio` has many `Transactions` (audit log of all buy/sell)

TimescaleDB hypertable (time-series optimized):
- `OhlcvBars` table — partitioned by time automatically

---

## 8. External API Abstraction

All external data sources implement `IStockDataProvider`:
```csharp
public interface IStockDataProvider
{
    Task<StockTick> GetQuoteAsync(string symbol);
    Task<List<OhlcvBar>> GetOhlcvAsync(string symbol, string interval, DateTime from, DateTime to);
    Task<StockInfo> GetStockInfoAsync(string symbol);
    Task<List<string>> SearchSymbolsAsync(string query);
}
```

This means swapping from Yahoo Finance to Polygon.io requires:
1. Create `PolygonProvider.cs` implementing `IStockDataProvider`
2. Change one line in `DependencyInjection.cs`
3. Zero changes to any controller, hub, or frontend code

---

## 9. Error Handling

### API Layer
- `GlobalExceptionHandlerMiddleware` catches all unhandled exceptions
- Returns RFC 7807 Problem Details JSON:
```json
{
  "type": "https://stocksight.dev/errors/stock-not-found",
  "title": "Stock not found",
  "status": 404,
  "detail": "Symbol 'INVALID' was not found.",
  "traceId": "00-abc123..."
}
```

### External API Resilience
- `Polly` retry policy: 3 retries with exponential backoff (1s, 2s, 4s)
- Circuit breaker: open after 5 consecutive failures, half-open after 30s
- Fallback: return last known cached value if available, else throw

### Frontend
- `ErrorBoundary` component wraps each page
- Toast notification for user-facing errors
- Offline detection banner when SignalR disconnects
