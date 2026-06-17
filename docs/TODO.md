# TODO — StockSight

> Active task tracker. Update this file daily. Move completed items to DONE section.  
> Format: `- [ ]` open · `- [x]` done · `- [-]` blocked (add reason inline)

---

## Current Sprint — Phase 4: Portfolio, Alerts, Auth & News

### In Progress
_None — Phase 3 is implemented._

### Up Next
- [ ] Start Phase 4 portfolio, alerts, auth, and news

### Blocked
_None yet_

---

## Phase 1 Checklist

### Solution & Projects
- [x] `dotnet new sln -n StockSight`
- [x] Create `StockSight.Core` class library
- [x] Create `StockSight.API` web API project
- [x] Create `StockSight.Infrastructure` class library
- [x] Create `StockSight.Web` Blazor WASM project
- [x] Create `StockSight.Tests` xUnit project
- [x] Add all projects to solution
- [x] Set project references correctly
- [x] Add `.gitignore`
- [x] Create `docs/` folder and copy all markdown files
- [x] Initial commit pushed to GitHub

### Domain Models (`StockSight.Core/Models/`)
- [x] `StockTick.cs`
- [x] `OhlcvBar.cs`
- [x] `StockInfo.cs`
- [x] `User.cs`
- [x] `WatchlistItem.cs`
- [x] `Portfolio.cs`
- [x] `PortfolioPosition.cs`
- [x] `PriceAlert.cs`
- [x] `TradeSignal.cs`

### Interfaces (`StockSight.Core/Interfaces/`)
- [x] `IStockDataProvider.cs`
- [x] `ISignalEngine.cs`
- [x] `ICacheService.cs`
- [x] `IAlertService.cs`
- [x] `IPortfolioService.cs`
- [x] `INewsService.cs`

### Infrastructure — Data
- [x] Install NuGet packages (YahooFinanceApi, RestSharp, OpenAI)
- [x] `YahooFinanceProvider.cs` — GetQuoteAsync
- [x] `YahooFinanceProvider.cs` — GetOhlcvAsync
- [x] `YahooFinanceProvider.cs` — GetStockInfoAsync
- [ ] `AlphaVantageProvider.cs` — GetRsiAsync (fallback)

### Infrastructure — Database
- [x] Install EF Core + Npgsql packages
- [x] `StockSightDbContext.cs` with all DbSets
- [x] Entity configurations (Fluent API)
- [x] First migration: `InitialCreate`
- [x] Seed 5 default symbols
- [ ] Test connection to local PostgreSQL

### Infrastructure — Redis
- [x] Install StackExchange.Redis
- [x] `RedisCacheService.cs` — GetAsync, SetAsync
- [x] `RedisCacheService.cs` — PublishAsync, SubscribeAsync
- [ ] Test Redis connection

### API — Background Service
- [x] `StockDataIngestionService.cs` extends BackgroundService
- [x] Fetch loop every 5 seconds
- [x] Push to Redis pub/sub
- [x] Register in `Program.cs`

### API — SignalR
- [x] `StockHub.cs` with Subscribe/Unsubscribe methods
- [x] Register SignalR + CORS in `Program.cs`
- [x] Map hub route `/hubs/stocks`
- [ ] Test with browser SignalR client

### API — REST Endpoints
- [x] `GET /api/stocks/{symbol}/quote`
- [x] `GET /api/stocks/{symbol}/ohlcv`
- [x] `GET /api/stocks/{symbol}/info`
- [x] `GET /api/stocks/search?q=`
- [x] `GET /health`
- [ ] Test all with Swagger / HTTP file

### Frontend — Blazor Shell
- [x] Install SignalR client NuGet
- [x] `StockHubService.cs` connection wrapper
- [x] `MainLayout.razor` — sidebar nav + top bar
- [x] `WatchlistPage.razor` — list with live price badges
- [ ] Confirm live updates visible in browser

---

## Phase 2 Checklist (starts after Phase 1 complete)

- [x] TradingView Lightweight Charts via CDN
- [x] `ChartComponent.razor` + JS interop file
- [x] `initChart`, `loadOhlcvData`, `addRealtimeTick` JS functions
- [x] Interval selector (1m, 5m, 15m, 1h, 4h, 1D, 1W)
- [x] `IndicatorCalculator.cs` — SMA, EMA, RSI, MACD, Bollinger
- [x] Unit tests for all indicator calculations
- [x] Indicator API endpoints
- [x] Toggle panel for indicator overlays
- [x] RSI and MACD render in sub-pane below main chart
- [x] Full stock detail page

---

## Phase 3 Checklist (starts after Phase 2 complete)

- [x] `SignalEngine.cs` — combined rule + sentiment
- [x] `RuleBasedSignalAnalyzer.cs` — RSI, MACD, Bollinger rules
- [x] `NewsSentimentAnalyzer.cs` — OpenAI integration
- [x] `SignalBadge.razor` component
- [x] `BacktestEngine.cs` with metrics calculation
- [x] 3 built-in strategies (SMA crossover, RSI reversal, MACD)
- [x] Backtest API endpoint
- [x] `BacktestPage.razor` with equity curve chart

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

- [x] 2026-06-17 — Phase 1 foundation implemented: Core models/interfaces, Yahoo provider, Redis cache/pub-sub, EF migration + seed data, SignalR hub, ingestion background service, REST stock endpoints, health endpoint, Blazor watchlist, and GitHub Actions CI.
- [x] 2026-06-17 — Phase 2 chart foundation implemented: TradingView Lightweight Charts interop, stock detail page, interval selector, SMA/EMA/Bollinger overlays, RSI/MACD summary, indicator API endpoints, fallback market data, and indicator unit tests.
- [x] 2026-06-17 — Phase 2 completed: RSI line pane with 30/70 guide lines and MACD pane with MACD/signal lines plus histogram.
- [x] 2026-06-17 — Phase 3 completed: combined technical/news signal engine, OpenAI-ready sentiment analyzer with fallback, signal badge/history UI, backtest engine, SMA/RSI/MACD strategies, backtest API, backtest page, and 20 passing tests.

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
