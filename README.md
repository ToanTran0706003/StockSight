# StockSight

Real-time stock dashboard built with **ASP.NET Core 8 + Blazor WebAssembly + SignalR**, organised as a Clean Architecture solution.

Phase 1 is implemented: the API can fetch/cache quotes, the background ingestion service polls configured symbols, SignalR broadcasts ticks, and the Blazor watchlist renders live price badges.

Phase 2 chart foundation is implemented: stock detail pages render candlesticks with TradingView Lightweight Charts, interval switching, SMA/EMA/Bollinger overlays, RSI/MACD summaries, and server-side indicator endpoints.

## Architecture

```
StockSight/
├── StockSight.sln
├── src/
│   ├── StockSight.API/            ASP.NET Core 8 Web API — controllers, SignalR hub, Hangfire, DI host
│   ├── StockSight.Core/           Domain models (StockTick, Portfolio, Alert) + interfaces (no external deps)
│   ├── StockSight.Infrastructure/ Redis cache, EF Core/PostgreSQL DbContext, Yahoo Finance provider
│   └── StockSight.Web/            Blazor WebAssembly client (SignalR live ticker)
└── tests/
    └── StockSight.Tests/          xUnit + Moq unit tests
```

Dependency flow: `API → Infrastructure → Core`, `Web → Core`, `Tests → API/Infrastructure/Core`.
Core has no third-party dependencies, so both the server and the Blazor client share the same models.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- PostgreSQL (default conn: `Host=localhost;Database=stocksight;Username=postgres;Password=postgres`)
- Redis (default `localhost:6379`)

The quickest way to get the dependencies running locally:

```bash
docker run -d --name stocksight-pg  -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=stocksight -p 5432:5432 postgres:16
docker run -d --name stocksight-redis -p 6379:6379 redis:7
```

## First build

This solution was generated with file contents only (no `dotnet new`), so restore packages on first use:

```bash
dotnet restore
dotnet build
```

> NuGet package versions are pinned in each `.csproj`. If a version is unavailable
> for your feed, run `dotnet restore` and bump to the nearest 8.0.x release.

## Database migrations

```bash
# from repo root
dotnet tool install --global dotnet-ef          # once
dotnet ef migrations add InitialCreate \
    --project src/StockSight.Infrastructure \
    --startup-project src/StockSight.API
dotnet ef database update \
    --project src/StockSight.Infrastructure \
    --startup-project src/StockSight.API
```

## Run

```bash
# Terminal 1 — API (https://localhost:7080, Swagger at /swagger, Hangfire at /hangfire)
dotnet run --project src/StockSight.API

# Terminal 2 — Blazor client (https://localhost:7000)
dotnet run --project src/StockSight.Web
```

Open the client, go to **Live Ticker**, and subscribe to a symbol. The page connects
to the `/hubs/stocks` SignalR hub and renders `ReceiveTick` messages.

## Key endpoints

| Endpoint                  | Purpose                                             |
|---------------------------|-----------------------------------------------------|
| `GET /api/stocks/{symbol}`| Latest quote (Redis cache-aside over Yahoo Finance) |
| `/hubs/stocks`            | SignalR hub — `Subscribe` / `Unsubscribe` + `ReceiveTick` |
| `/swagger`                | OpenAPI UI (Development)                             |
| `/hangfire`               | Background job dashboard                             |

## Configuration

`src/StockSight.API/appsettings.json`:

- `ConnectionStrings:Postgres` — EF Core + Hangfire storage
- `Redis:ConnectionString` — StackExchange.Redis endpoint
- `Cors:AllowedOrigins` — Blazor client origins
- `OpenAI:ApiKey` — for AI features (left blank by default)

`src/StockSight.Web/wwwroot/appsettings.json`:

- `ApiBaseUrl` — base address of the API the client talks to

## Tests

```bash
dotnet test
```

## What's wired vs. what's a stub

**Wired:** project structure & references, NuGet packages, Phase 1 domain models,
SignalR hub + broadcaster, Redis cache/pub-sub service, EF Core/PostgreSQL DbContext
with `InitialCreate` migration and seed symbols, Hangfire server, CORS, Swagger,
cache-aside stock endpoints, health endpoint, background quote ingestion, Blazor
watchlist/live-ticker pages, GitHub Actions CI, and unit tests.

**Stub / next steps:** RSI/MACD sub-pane charts, alert evaluation pipeline,
portfolio CRUD endpoints, authentication, backtesting, and the OpenAI-powered insights service.
