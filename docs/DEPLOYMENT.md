# Deployment Guide — StockSight

> Full deployment to production using only free-tier services. Total cost: $0/month.

---

## Services Overview

| What | Where | URL after deploy |
|---|---|---|
| ASP.NET Core API | Railway.app | `https://stocksight-api.railway.app` |
| Blazor WASM frontend | Netlify | `https://stocksight.netlify.app` |
| PostgreSQL database | Supabase | (internal connection string) |
| Redis cache | Upstash | (internal connection string) |
| Email | SendGrid | (API key only) |

---

## Step 1 — Supabase (PostgreSQL)

1. Go to [supabase.com](https://supabase.com) and sign up (free, no credit card)
2. Create new project → choose region closest to you
3. Wait ~2 minutes for provisioning
4. Go to **Settings → Database**
5. Copy the **Connection string** (URI format):
   ```
   postgresql://postgres:[PASSWORD]@db.[REF].supabase.co:5432/postgres
   ```
6. Enable TimescaleDB:
   - Go to **SQL Editor** in Supabase dashboard
   - Run: `CREATE EXTENSION IF NOT EXISTS timescaledb;`
   - Run: `SELECT create_hypertable('ohlcv_bars', 'timestamp');` (after migrations applied)

---

## Step 2 — Upstash (Redis)

1. Go to [upstash.com](https://upstash.com) and sign up (free, no credit card)
2. Create new Redis database → select region
3. Copy the **Redis URL**:
   ```
   rediss://default:[PASSWORD]@[HOST].upstash.io:6379
   ```
4. Note: Upstash uses TLS (`rediss://` not `redis://`) — configure StackExchange.Redis accordingly:
   ```csharp
   var config = ConfigurationOptions.Parse(redisUrl);
   config.Ssl = true;
   config.SslProtocols = System.Security.Authentication.SslProtocols.Tls12;
   ```

---

## Step 3 — SendGrid (Email alerts)

1. Go to [sendgrid.com](https://sendgrid.com) and sign up (free 100 emails/day)
2. Complete sender verification (verify your domain or single sender email)
3. Go to **Settings → API Keys → Create API Key**
4. Name it `StockSight`, select **Restricted Access → Mail Send: Full Access**
5. Copy the API key (shown only once)

---

## Step 4 — External API Keys

### Alpha Vantage (free)
1. Go to [alphavantage.co/support](https://www.alphavantage.co/support/#api-key)
2. Enter email → get instant free API key
3. Free limit: 25 requests/day (sufficient for demo)

### NewsAPI (free)
1. Go to [newsapi.org/register](https://newsapi.org/register)
2. Sign up → get API key instantly
3. Free limit: 100 requests/day

### OpenAI (optional, ~$0 with free credit)
1. Go to [platform.openai.com](https://platform.openai.com)
2. Sign up → new accounts get $5 free credit
3. Go to **API Keys → Create new secret key**
4. Copy key (shown only once)

### Polygon.io (optional)
1. Go to [polygon.io](https://polygon.io) and sign up
2. Free tier: unlimited historical data, 5 API calls/min

---

## Step 5 — Railway.app (API Hosting)

### Setup
1. Go to [railway.app](https://railway.app) and sign up with GitHub
2. Click **New Project → Deploy from GitHub repo**
3. Select your `StockSight` repository
4. Railway auto-detects .NET — set the **Root Directory** to `src/StockSight.API`

### Environment Variables
In Railway dashboard → your service → **Variables**, add:

```
ASPNETCORE_ENVIRONMENT=Production
ConnectionStrings__Postgres=postgresql://postgres:...@db....supabase.co:5432/postgres
ConnectionStrings__Redis=rediss://default:...@....upstash.io:6379
ApiKeys__AlphaVantage=YOUR_KEY
ApiKeys__NewsApi=YOUR_KEY
ApiKeys__OpenAI=YOUR_KEY
ApiKeys__SendGrid=YOUR_KEY
Jwt__SecretKey=GENERATE_A_RANDOM_64_CHAR_STRING
Jwt__Issuer=https://stocksight-api.railway.app
Jwt__Audience=https://stocksight.netlify.app
Cors__AllowedOrigins=https://stocksight.netlify.app
```

### Generate JWT secret key
```bash
# PowerShell
[Convert]::ToBase64String([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(64))

# or Node.js
node -e "console.log(require('crypto').randomBytes(64).toString('base64'))"
```

### Run database migrations on Railway
After first deploy, go to Railway → your service → **Shell**:
```bash
dotnet ef database update --project StockSight.Infrastructure --startup-project StockSight.API
```

Or add a migration runner to `Program.cs` (runs on startup):
```csharp
// In Program.cs, before app.Run()
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StockSightDbContext>();
    db.Database.Migrate();
}
```

### Dockerfile (for Railway)
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/StockSight.API/StockSight.API.csproj", "src/StockSight.API/"]
COPY ["src/StockSight.Core/StockSight.Core.csproj", "src/StockSight.Core/"]
COPY ["src/StockSight.Infrastructure/StockSight.Infrastructure.csproj", "src/StockSight.Infrastructure/"]
RUN dotnet restore "src/StockSight.API/StockSight.API.csproj"
COPY . .
RUN dotnet build "src/StockSight.API/StockSight.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/StockSight.API/StockSight.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "StockSight.API.dll"]
```

---

## Step 6 — Netlify (Blazor WASM Frontend)

### Build settings
Blazor WASM publishes to static files — Netlify hosts these for free.

1. Go to [netlify.com](https://netlify.com) and sign up with GitHub
2. Click **Add new site → Import an existing project → GitHub**
3. Select `StockSight` repository

**Build settings:**
```
Base directory:    src/StockSight.Web
Build command:     dotnet publish -c Release -o publish
Publish directory: src/StockSight.Web/publish/wwwroot
```

4. Add environment variable in Netlify → **Site settings → Environment variables**:
```
STOCKSIGHT_API_URL=https://stocksight-api.railway.app
```

### Blazor WASM routing fix (required)
Create `src/StockSight.Web/wwwroot/_redirects`:
```
/*    /index.html   200
```

This tells Netlify to serve `index.html` for all routes (Blazor handles routing client-side).

---

## Step 7 — Update CORS in API

In `appsettings.json` (or Railway env vars), set:
```json
{
  "Cors": {
    "AllowedOrigins": ["https://stocksight.netlify.app"]
  }
}
```

In `Program.cs`:
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(builder.Configuration["Cors:AllowedOrigins"]!)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // required for SignalR + httpOnly cookies
    });
});
```

---

## Step 8 — Verify Deployment

### Checklist
- [ ] `GET https://stocksight-api.railway.app/health` returns `{"status": "Healthy"}`
- [ ] `https://stocksight.netlify.app` loads the Blazor app
- [ ] Login/register works
- [ ] Stock prices load on watchlist page
- [ ] Real-time updates tick in browser (SignalR connected)
- [ ] Chart loads on stock detail page
- [ ] Backtest runs and returns results
- [ ] Price alert sends email when triggered

---

## Local Development (Docker Compose)

```yaml
# docker-compose.yml
version: '3.8'
services:
  postgres:
    image: timescale/timescaledb:latest-pg15
    ports:
      - "5432:5432"
    environment:
      POSTGRES_PASSWORD: postgres
      POSTGRES_DB: stocksight
    volumes:
      - postgres_data:/var/lib/postgresql/data

  redis:
    image: redis:7-alpine
    ports:
      - "6379:6379"

volumes:
  postgres_data:
```

```bash
# Start local infrastructure
docker-compose up -d

# Run API
cd src/StockSight.API
dotnet run

# Run Blazor (separate terminal)
cd src/StockSight.Web
dotnet run
```

---

## Monitoring (Free)

- **Railway** — built-in logs viewer, CPU/memory metrics
- **Supabase** — query performance dashboard, table editor
- **Upstash** — Redis command count, bandwidth usage
- **UptimeRobot** (free) — ping `/health` every 5 minutes, email alert if down

Add UptimeRobot monitor at [uptimerobot.com](https://uptimerobot.com) (free, 50 monitors):
- URL: `https://stocksight-api.railway.app/health`
- Interval: 5 minutes
- Alert contact: your email
