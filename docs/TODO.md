# TODO — StockSight

> Active task tracker. Update this file daily. Move completed items to DONE section.  
> Format: `- [ ]` open · `- [x]` done · `- [-]` blocked (add reason inline)

---

## Current Sprint — Phase 1: Foundation

### In Progress
- [ ] Solution scaffold and project references
- [ ] Core domain models

### Up Next
- [ ] SignalR Hub setup
- [ ] Yahoo Finance data provider
- [ ] Redis cache service
- [ ] Background ingestion service
- [ ] Blazor shell + SignalR client

### Blocked
_None yet_

---

## Phase 1 Checklist

### Solution & Projects
- [ ] `dotnet new sln -n StockSight`
- [ ] Create `StockSight.Core` class library
- [ ] Create `StockSight.API` web API project
- [ ] Create `StockSight.Infrastructure` class library
- [ ] Create `StockSight.Web` Blazor WASM project
- [ ] Create `StockSight.Tests` xUnit project
- [ ] Add all projects to solution
- [ ] Set project references correctly
- [ ] Add `.gitignore`
- [ ] Create `docs/` folder and copy all markdown files
- [ ] Initial commit pushed to GitHub

### Domain Models (`StockSight.Core/Models/`)
- [ ] `StockTick.cs`
- [ ] `OhlcvBar.cs`
- [ ] `StockInfo.cs`
- [ ] `User.cs`
- [ ] `WatchlistItem.cs`
- [ ] `Portfolio.cs`
- [ ] `PortfolioPosition.cs`
- [ ] `PriceAlert.cs`
- [ ] `TradeSignal.cs`

### Interfaces (`StockSight.Core/Interfaces/`)
- [ ] `IStockDataProvider.cs`
- [ ] `ISignalEngine.cs`
- [ ] `ICacheService.cs`
- [ ] `IAlertService.cs`
- [ ] `IPortfolioService.cs`
- [ ] `INewsService.cs`

### Infrastructure — Data
- [ ] Install NuGet packages (YahooFinanceApi, RestSharp, Newtonsoft.Json)
- [ ] `YahooFinanceProvider.cs` — GetQuoteAsync
- [ ] `YahooFinanceProvider.cs` — GetOhlcvAsync
- [ ] `YahooFinanceProvider.cs` — GetStockInfoAsync
- [ ] `AlphaVantageProvider.cs` — GetRsiAsync (fallback)

### Infrastructure — Database
- [ ] Install EF Core + Npgsql packages
- [ ] `StockSightDbContext.cs` with all DbSets
- [ ] Entity configurations (Fluent API)
- [ ] First migration: `InitialCreate`
- [ ] Seed 5 default symbols
- [ ] Test connection to local PostgreSQL

### Infrastructure — Redis
- [ ] Install StackExchange.Redis
- [ ] `RedisCacheService.cs` — GetAsync, SetAsync
- [ ] `RedisCacheService.cs` — PublishAsync, SubscribeAsync
- [ ] Test Redis connection

### API — Background Service
- [ ] `StockDataIngestionService.cs` extends BackgroundService
- [ ] Fetch loop every 5 seconds
- [ ] Push to Redis pub/sub
- [ ] Register in `Program.cs`

### API — SignalR
- [ ] `StockHub.cs` with Subscribe/Unsubscribe methods
- [ ] Register SignalR + CORS in `Program.cs`
- [ ] Map hub route `/hubs/stock`
- [ ] Test with browser SignalR client

### API — REST Endpoints
- [ ] `GET /api/stocks/{symbol}/quote`
- [ ] `GET /api/stocks/{symbol}/ohlcv`
- [ ] `GET /api/stocks/{symbol}/info`
- [ ] `GET /api/stocks/search?q=`
- [ ] `GET /health`
- [ ] Test all with Swagger / HTTP file

### Frontend — Blazor Shell
- [ ] Install SignalR client NuGet
- [ ] `StockHubService.cs` connection wrapper
- [ ] `MainLayout.razor` — sidebar nav + top bar
- [ ] `WatchlistPage.razor` — list with live price badges
- [ ] Confirm live updates visible in browser

---

## Phase 2 Checklist (starts after Phase 1 complete)

- [ ] TradingView Lightweight Charts via CDN
- [ ] `ChartComponent.razor` + JS interop file
- [ ] `initChart`, `loadOhlcvData`, `addRealtimeTick` JS functions
- [ ] Interval selector (1m, 5m, 15m, 1h, 1D)
- [ ] `IndicatorCalculator.cs` — SMA, EMA, RSI, MACD, Bollinger
- [ ] Unit tests for all indicator calculations
- [ ] Indicator API endpoints
- [ ] Toggle panel for indicator overlays
- [ ] Full stock detail page

---

## Phase 3 Checklist (starts after Phase 2 complete)

- [ ] `SignalEngine.cs` — combined rule + sentiment
- [ ] `RuleBasedSignalAnalyzer.cs` — RSI, MACD, Bollinger rules
- [ ] `NewsSentimentAnalyzer.cs` — OpenAI integration
- [ ] `SignalBadge.razor` component
- [ ] `BacktestEngine.cs` with metrics calculation
- [ ] 3 built-in strategies (SMA crossover, RSI reversal, MACD)
- [ ] Backtest API endpoint
- [ ] `BacktestPage.razor` with equity curve chart

---

## Phase 4 Checklist (starts after Phase 3 complete)

- [ ] JWT auth (register + login)
- [ ] `PortfolioService.cs` + controller
- [ ] `PortfolioPage.razor` with real-time P&L
- [ ] `AlertCheckerService.cs` background service
- [ ] `AlertsPage.razor`
- [ ] `NewsService.cs` + sentiment
- [ ] `NewsPanel.razor`
- [ ] Watchlist CRUD endpoints

---

## Phase 5 Checklist (starts after Phase 4 complete)

- [ ] 20+ unit tests passing
- [ ] Integration tests for main API flows
- [ ] Global error handling middleware
- [ ] Polly retry policy for external APIs
- [ ] Mock data fallback service
- [ ] `docker-compose.yml`
- [ ] `Dockerfile` for API
- [ ] GitHub Actions CI pipeline
- [ ] Deploy to Railway + Netlify + Supabase + Upstash
- [ ] README with demo GIF
- [ ] Clean commit history
- [ ] GitHub repo topics and description

---

## Done ✅

_Move completed items here with date_

---

## Backlog (Not in current scope)

- Dark mode toggle
- Export portfolio to PDF
- Multi-symbol comparison chart
- ML.NET price prediction model
- Minimal APIs refactor
- API versioning
- Rate limiting middleware
- WebAssembly SIMD for indicators

---

## Notes / Decisions Log

| Date | Decision | Reason |
|---|---|---|
| - | Use Yahoo Finance unofficial API first | No API key needed, faster to prototype |
| - | Blazor WASM over React | Full C# stack, better for C# portfolio |
| - | Redis via Upstash free tier | No credit card needed for free tier |
| - | JWT stored in memory (not localStorage) | Security best practice for Blazor WASM |
