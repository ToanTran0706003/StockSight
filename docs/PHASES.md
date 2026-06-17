# Phases & Roadmap — StockSight

> Each phase builds on the previous. Never skip a phase. Each phase ends with a working, committable state.

---

## Phase 1 — Foundation & Real-time Core
**Duration:** Week 1–2  
**Goal:** Project scaffolded, data flowing, SignalR working end-to-end

### 1.1 Solution Setup
- [ ] Create solution `StockSight.sln`
- [ ] Create project `StockSight.Core` (Class Library)
- [ ] Create project `StockSight.API` (ASP.NET Core 8 Web API)
- [ ] Create project `StockSight.Infrastructure` (Class Library)
- [ ] Create project `StockSight.Web` (Blazor WebAssembly)
- [ ] Create project `StockSight.Tests` (xUnit)
- [ ] Add all projects to solution
- [ ] Setup project references (API → Core ← Infrastructure)
- [ ] Add `.gitignore` (Visual Studio template)
- [ ] Initial commit: "chore: initial solution scaffold"

### 1.2 Core Domain Models
Location: `StockSight.Core/Models/`
- [ ] `StockTick.cs` — Symbol, Price, Volume, Timestamp, Change, ChangePercent
- [ ] `OhlcvBar.cs` — Open, High, Low, Close, Volume, Timestamp, Interval
- [ ] `StockInfo.cs` — Symbol, CompanyName, Sector, MarketCap, PE, DividendYield
- [ ] `User.cs` — Id, Email, PasswordHash, CreatedAt
- [ ] `WatchlistItem.cs` — UserId, Symbol, AddedAt
- [ ] `Portfolio.cs` — Id, UserId, Name, CreatedAt
- [ ] `PortfolioPosition.cs` — PortfolioId, Symbol, Shares, AverageCost, BoughtAt
- [ ] `PriceAlert.cs` — Id, UserId, Symbol, TargetPrice, Direction (Above/Below), IsTriggered
- [ ] `TradeSignal.cs` — Symbol, Action (BUY/SELL/HOLD), Confidence, Reason, GeneratedAt

### 1.3 Core Interfaces
Location: `StockSight.Core/Interfaces/`
- [ ] `IStockDataProvider.cs` — GetQuoteAsync, GetOhlcvAsync, GetStockInfoAsync
- [ ] `ISignalEngine.cs` — AnalyzeAsync(symbol) → TradeSignal
- [ ] `ICacheService.cs` — GetAsync, SetAsync, DeleteAsync, PublishAsync, SubscribeAsync
- [ ] `IAlertService.cs` — CheckAlertsAsync, CreateAlertAsync, DeleteAlertAsync
- [ ] `IPortfolioService.cs` — GetPortfolioAsync, BuyAsync, SellAsync, GetPnLAsync
- [ ] `INewsService.cs` — GetNewsBySymbolAsync, GetSentimentAsync

### 1.4 Infrastructure — Data Providers
Location: `StockSight.Infrastructure/DataProviders/`
- [ ] Install NuGet: `YahooFinanceApi`, `RestSharp`, `Newtonsoft.Json`
- [ ] `YahooFinanceProvider.cs` implements `IStockDataProvider`
  - [ ] `GetQuoteAsync(string symbol)` → StockTick
  - [ ] `GetOhlcvAsync(string symbol, string interval, DateTime from, DateTime to)` → List\<OhlcvBar\>
  - [ ] `GetStockInfoAsync(string symbol)` → StockInfo
- [ ] `AlphaVantageProvider.cs` — fallback for indicators
  - [ ] `GetRsiAsync(symbol, period)` → List\<decimal\>
  - [ ] `GetMacdAsync(symbol)` → MacdResult

### 1.5 Infrastructure — Database
Location: `StockSight.Infrastructure/Persistence/`
- [ ] Install NuGet: `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `EFCore.NamingConventions`
- [ ] `StockSightDbContext.cs` — DbSets for all entities
- [ ] `UserConfiguration.cs` — Fluent API config
- [ ] `PortfolioConfiguration.cs`
- [ ] `PriceAlertConfiguration.cs`
- [ ] Initial migration: `dotnet ef migrations add InitialCreate`
- [ ] Seed data: 5 default watchlist symbols (AAPL, GOOGL, MSFT, TSLA, AMZN)

### 1.6 Infrastructure — Redis
Location: `StockSight.Infrastructure/Cache/`
- [ ] Install NuGet: `StackExchange.Redis`
- [ ] `RedisCacheService.cs` implements `ICacheService`
  - [ ] Cache tick data with 5-second TTL
  - [ ] Pub/sub channel per symbol: `tick:{SYMBOL}`

### 1.7 API — Data Ingestion Background Service
Location: `StockSight.API/BackgroundServices/`
- [ ] `StockDataIngestionService.cs` extends `BackgroundService`
  - [ ] Fetch quotes for all watched symbols every 5 seconds (market hours only)
  - [ ] Push to Redis pub/sub
  - [ ] Trigger SignalR broadcast

### 1.8 API — SignalR Hub
Location: `StockSight.API/Hubs/`
- [ ] `StockHub.cs` extends `Hub`
  - [ ] `SubscribeToSymbol(string symbol)` — add to SignalR group
  - [ ] `UnsubscribeFromSymbol(string symbol)` — leave group
  - [ ] Server-to-client: `ReceiveTick(StockTick tick)`
  - [ ] Server-to-client: `ReceiveSignal(TradeSignal signal)`
- [ ] Register SignalR in `Program.cs`
- [ ] Map hub at `/hubs/stock`
- [ ] Enable CORS for Blazor client origin

### 1.9 API — Basic Endpoints
Location: `StockSight.API/Controllers/`
- [ ] `StocksController.cs`
  - [ ] `GET /api/stocks/{symbol}/quote`
  - [ ] `GET /api/stocks/{symbol}/ohlcv?interval=1d&from=&to=`
  - [ ] `GET /api/stocks/{symbol}/info`
  - [ ] `GET /api/stocks/search?q=`
- [ ] `HealthController.cs`
  - [ ] `GET /health` — returns API status + Redis status + DB status

### 1.10 Frontend — Blazor Shell
Location: `StockSight.Web/`
- [ ] Install NuGet: `Microsoft.AspNetCore.SignalR.Client`
- [ ] Setup `HttpClient` base address in `Program.cs`
- [ ] `StockHubService.cs` — wrapper around `HubConnection`
  - [ ] Connect on app start
  - [ ] Reconnect with exponential backoff
  - [ ] Expose `OnTickReceived` event
- [ ] Basic layout: `MainLayout.razor` with sidebar + top bar
- [ ] `WatchlistPage.razor` — list of symbols with live price badges
- [ ] Confirm prices update live in browser

**Phase 1 Deliverable:** Open browser, see live price updates ticking in real-time. No charts yet.

---

## Phase 2 — Charts & Technical Indicators
**Duration:** Week 3–4  
**Goal:** Professional candlestick charts with indicator overlays

### 2.1 TradingView Charts Integration
- [ ] Add TradingView Lightweight Charts via CDN in `wwwroot/index.html`
- [ ] Create `ChartComponent.razor` with JS interop
- [ ] `wwwroot/js/chart-interop.js`
  - [ ] `initChart(elementId)` — create chart instance
  - [ ] `loadOhlcvData(data)` — populate candlestick series
  - [ ] `addRealtimeTick(tick)` — update last candle or add new candle
  - [ ] `addIndicatorLine(name, data, color)` — overlay MA, etc.
- [ ] `IJSRuntime` calls from Blazor component

### 2.2 Interval Selector
- [ ] Interval buttons: 1m, 5m, 15m, 1h, 4h, 1D, 1W
- [ ] On change: fetch new OHLCV, re-render chart
- [ ] Active interval highlighted

### 2.3 Technical Indicators (Server-side calculation)
Location: `StockSight.Core/Indicators/`
- [ ] `IndicatorCalculator.cs` (static helper class)
  - [ ] `CalculateSma(prices, period)` → List\<decimal?\>
  - [ ] `CalculateEma(prices, period)` → List\<decimal?\>
  - [ ] `CalculateRsi(prices, period = 14)` → List\<decimal?\>
  - [ ] `CalculateMacd(prices, fast=12, slow=26, signal=9)` → MacdResult
  - [ ] `CalculateBollingerBands(prices, period=20, stdDev=2)` → BollingerResult
- [ ] Unit tests for each indicator (compare against known values)

### 2.4 Indicator API Endpoints
- [ ] `GET /api/stocks/{symbol}/indicators/rsi?period=14`
- [ ] `GET /api/stocks/{symbol}/indicators/macd`
- [ ] `GET /api/stocks/{symbol}/indicators/bollinger`
- [ ] `GET /api/stocks/{symbol}/indicators/sma?period=20`

### 2.5 Chart Overlay Toggle UI
- [ ] Checkbox panel: SMA 20, EMA 50, RSI, MACD, Bollinger Bands
- [ ] Toggle adds/removes indicator from chart
- [ ] RSI and MACD render in sub-pane below main chart

### 2.6 Stock Detail Page
- [ ] Route: `/stock/{symbol}`
- [ ] Header: company name, current price, change %, market cap
- [ ] Full-width candlestick chart
- [ ] Indicator panel
- [ ] Key stats sidebar: PE, volume, 52w high/low, dividend yield

**Phase 2 Deliverable:** Click any symbol, see professional candlestick chart with indicator overlays updating live.

---

## Phase 3 — AI Signals & Backtesting
**Duration:** Week 5–7  
**Goal:** The most impressive feature — AI says when to buy/sell, plus historical proof

### 3.1 AI Signal Engine
Location: `StockSight.Infrastructure/AI/`
- [ ] Install NuGet: `Microsoft.ML`, `OpenAI` (or use `HttpClient` directly)
- [ ] `SignalEngine.cs` implements `ISignalEngine`
  - [ ] `AnalyzeAsync(string symbol)` → TradeSignal
  - [ ] Step 1: Fetch last 50 candles + current indicators
  - [ ] Step 2: Apply rule-based signals (RSI oversold/overbought, MACD crossover, BB squeeze)
  - [ ] Step 3: Call OpenAI to get sentiment context on recent news
  - [ ] Step 4: Combine into final signal with confidence (0–100%)

### 3.2 Rule-based Signal Logic
Location: `StockSight.Core/Signals/`
- [ ] `RuleBasedSignalAnalyzer.cs`
  - [ ] RSI < 30 → BUY signal (+weight)
  - [ ] RSI > 70 → SELL signal (+weight)
  - [ ] MACD line crosses above signal line → BUY
  - [ ] MACD line crosses below signal line → SELL
  - [ ] Price touches lower Bollinger Band → BUY
  - [ ] Price touches upper Bollinger Band → SELL
  - [ ] Combine weights → final action + confidence score

### 3.3 OpenAI Sentiment Integration
Location: `StockSight.Infrastructure/AI/`
- [ ] `NewsSentimentAnalyzer.cs`
  - [ ] Fetch last 5 news articles for symbol via NewsAPI
  - [ ] Send to OpenAI: "Given these headlines, is the sentiment for {SYMBOL} bullish, bearish, or neutral? Reply with JSON: {sentiment, score, reason}"
  - [ ] Parse response, add to signal as context
  - [ ] Cache result for 1 hour (news sentiment doesn't change by the minute)

### 3.4 Signal Display in Frontend
- [ ] `SignalBadge.razor` — renders BUY (green) / SELL (red) / HOLD (gray)
- [ ] Confidence meter (progress bar 0–100%)
- [ ] Reason tooltip on hover
- [ ] Signal history list on stock detail page (last 10 signals for that symbol)

### 3.5 Backtesting Engine
Location: `StockSight.Core/Backtesting/`
- [ ] `BacktestEngine.cs`
  - [ ] `RunAsync(IBacktestStrategy strategy, string symbol, DateTime from, DateTime to, decimal initialCapital)` → BacktestResult
  - [ ] Iterates over historical OHLCV bars
  - [ ] Calls `strategy.ShouldBuy(bars, index)` and `strategy.ShouldSell(bars, index)`
  - [ ] Tracks: positions, cash, equity curve
- [ ] `BacktestResult.cs`
  - [ ] TotalReturn (%)
  - [ ] SharpeRatio
  - [ ] MaxDrawdown (%)
  - [ ] WinRate (%)
  - [ ] TotalTrades
  - [ ] EquityCurve — List\<(DateTime, decimal)\>
- [ ] `IBacktestStrategy.cs` interface

### 3.6 Built-in Strategies
Location: `StockSight.Core/Backtesting/Strategies/`
- [ ] `SmaCrossoverStrategy.cs` — buy when SMA20 crosses above SMA50, sell when crosses below
- [ ] `RsiReversalStrategy.cs` — buy RSI < 30, sell RSI > 70
- [ ] `MacdStrategy.cs` — trade on MACD signal crossovers

### 3.7 Backtesting API & UI
- [ ] `POST /api/backtest` — body: symbol, strategy, from, to, capital → BacktestResult
- [ ] `BacktestPage.razor`
  - [ ] Form: symbol picker, strategy dropdown, date range, initial capital
  - [ ] Submit → show equity curve chart
  - [ ] Metrics cards: return, Sharpe, drawdown, win rate
  - [ ] Trade log table (date, action, price, shares, P&L)

**Phase 3 Deliverable:** Run a backtest, see how a strategy performed over the last year with full metrics.

---

## Phase 4 — Portfolio, Alerts & News
**Duration:** Week 8–9  
**Goal:** Complete the product feature set

### 4.1 User Authentication
- [ ] Install NuGet: `Microsoft.AspNetCore.Authentication.JwtBearer`, `BCrypt.Net-Next`
- [ ] `AuthController.cs`
  - [ ] `POST /api/auth/register`
  - [ ] `POST /api/auth/login` → returns JWT
  - [ ] `POST /api/auth/refresh`
- [ ] JWT middleware in `Program.cs`
- [ ] `[Authorize]` on portfolio, alert, watchlist endpoints
- [ ] Blazor: store JWT in memory (NOT localStorage), attach to HttpClient
- [ ] `LoginPage.razor`, `RegisterPage.razor`

### 4.2 Virtual Portfolio
- [ ] `PortfolioController.cs`
  - [ ] `GET /api/portfolio` — list portfolios
  - [ ] `POST /api/portfolio` — create portfolio (with initial virtual cash)
  - [ ] `POST /api/portfolio/{id}/buy` — buy shares (deduct virtual cash)
  - [ ] `POST /api/portfolio/{id}/sell` — sell shares
  - [ ] `GET /api/portfolio/{id}/pnl` — total P&L, position breakdown
- [ ] `PortfolioService.cs` — business logic, validates enough cash/shares
- [ ] `PortfolioPage.razor`
  - [ ] Overview: total value, total P&L (% and $), cash remaining
  - [ ] Positions table: symbol, shares, avg cost, current price, P&L per position
  - [ ] Allocation pie chart (using Chart.js via JS interop)
  - [ ] Buy/sell modal
- [ ] Real-time P&L updates via SignalR ticks

### 4.3 Price Alert System
- [ ] `AlertController.cs`
  - [ ] `GET /api/alerts` — list user's alerts
  - [ ] `POST /api/alerts` — create alert (symbol, target price, direction)
  - [ ] `DELETE /api/alerts/{id}` — remove alert
- [ ] `AlertCheckerService.cs` (background service)
  - [ ] Subscribe to Redis tick pub/sub
  - [ ] On each tick: check if any alert triggered
  - [ ] If triggered: send SignalR notification + email via SendGrid
- [ ] `AlertsPage.razor` — list alerts, create form, toggle active/inactive
- [ ] In-app notification toast when alert fires

### 4.4 News Sentiment Feed
- [ ] `NewsController.cs`
  - [ ] `GET /api/news/{symbol}` → latest 10 articles with sentiment scores
- [ ] `NewsService.cs`
  - [ ] Fetch from NewsAPI
  - [ ] Analyze each headline with OpenAI (batch, cache 1h)
  - [ ] Return: headline, source, publishedAt, url, sentiment, score
- [ ] `NewsPanel.razor` — shown on stock detail page
  - [ ] Article cards with sentiment badge (Bullish/Bearish/Neutral)
  - [ ] Color-coded score bar

### 4.5 Watchlist Management
- [ ] `WatchlistController.cs`
  - [ ] `GET /api/watchlist` — user's watchlist
  - [ ] `POST /api/watchlist/{symbol}` — add symbol
  - [ ] `DELETE /api/watchlist/{symbol}` — remove symbol
- [ ] Watchlist syncs with which symbols get polled in background service

**Phase 4 Deliverable:** Full working application — sign up, add stocks to watchlist, trade in virtual portfolio, set price alerts, read news with sentiment scores.

---

## Phase 5 — Polish, Tests & Deployment
**Duration:** Week 10–12  
**Goal:** Production-ready presentation, live demo, GitHub showcase

### 5.1 Unit Tests
Location: `StockSight.Tests/`
- [ ] `IndicatorCalculatorTests.cs` — RSI, MACD, SMA, EMA, Bollinger
- [ ] `SignalAnalyzerTests.cs` — rule combinations → expected signal
- [ ] `BacktestEngineTests.cs` — known historical data → expected return
- [ ] `PortfolioServiceTests.cs` — buy/sell logic, edge cases
- [ ] `AlertCheckerTests.cs` — trigger conditions
- [ ] Minimum 20 passing tests

### 5.2 Integration Tests
- [ ] `StocksControllerTests.cs` — API endpoint responses using `WebApplicationFactory`
- [ ] `AuthControllerTests.cs` — register + login flow

### 5.3 Error Handling & Resilience
- [ ] Global exception handler middleware
- [ ] `Polly` for API retry with exponential backoff
- [ ] Mock data fallback when external API rate-limited
- [ ] Friendly error messages in Blazor UI

### 5.4 Performance
- [ ] Enable Blazor WASM compression (`dotnet publish -c Release`)
- [ ] Lazy-load heavy pages (backtesting, charts)
- [ ] Redis caching audit — ensure all repeated API calls are cached
- [ ] API response compression middleware

### 5.5 Docker & Local Dev
- [ ] `docker-compose.yml` — PostgreSQL + Redis + (optional) API
- [ ] `Dockerfile` for API project
- [ ] `.env.example` file documenting all required environment variables
- [ ] `docker-compose.override.yml` for dev settings

### 5.6 CI/CD
Location: `.github/workflows/`
- [ ] `ci.yml` — on push to main: restore, build, test
- [ ] Badge in README: build passing ✅

### 5.7 Deployment
- [ ] Deploy PostgreSQL to Supabase (free tier)
- [ ] Deploy Redis to Upstash (free tier)
- [ ] Deploy API to Railway.app
- [ ] Deploy Blazor WASM to Netlify (publish output is static files)
- [ ] Set all environment variables in Railway dashboard
- [ ] Test live demo end-to-end

### 5.8 GitHub Presentation
- [ ] Professional README with:
  - [ ] Project description and motivation
  - [ ] Animated GIF or video demo (record with OBS or Loom)
  - [ ] Architecture diagram image
  - [ ] Tech stack badges (shields.io)
  - [ ] Live demo link
  - [ ] Quick start instructions
- [ ] Clean commit history (squash messy commits)
- [ ] GitHub repository description + topics: `csharp`, `dotnet`, `blazor`, `signalr`, `realtime`, `stocks`, `ai`
- [ ] Pin repository on GitHub profile

**Phase 5 Deliverable:** Live URL shared with recruiters. GitHub repo looks professional and complete.

---

## Backlog (Post v1.0)

Ideas to add after the main project is done — mention in README as "planned features":
- Dark mode toggle
- Export portfolio to CSV/PDF
- Multi-currency support
- Comparison chart (multiple symbols on same chart)
- ML.NET price prediction model (LSTM-style with time-series data)
- WebAssembly SIMD for faster indicator calculation
- Minimal API refactor (swap controllers for Minimal APIs)
- Rate limiting middleware
- API versioning
