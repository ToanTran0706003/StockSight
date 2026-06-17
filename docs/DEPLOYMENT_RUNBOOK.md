# StockSight Deployment Runbook

## Targets

- API: Railway using `Dockerfile.api`
- Web: Netlify using `netlify.toml`
- PostgreSQL: Supabase or Railway PostgreSQL
- Redis: Upstash or Railway Redis

## API Environment Variables

Use `.env.example` as the source of truth. At minimum set:

- `ConnectionStrings__Postgres`
- `Redis__ConnectionString`
- `Cors__AllowedOrigins__0`
- `Jwt__Secret`
- `OpenAI__ApiKey` when enabling OpenAI sentiment

## Smoke Checks

After deploy:

```bash
curl https://<api-host>/health
curl https://<api-host>/api/stocks/AAPL/quote
```

Then open the web app and verify:

- Register/login
- Watchlist loads quotes
- Stock detail renders chart, signal, and news sentiment
- Backtest runs
- Portfolio buy/sell updates P&L
- Alert create/delete works

## GitHub Topics

Recommended repo topics:

`csharp`, `dotnet`, `aspnetcore`, `blazor`, `signalr`, `realtime`, `stocks`, `ai`, `postgresql`, `redis`
