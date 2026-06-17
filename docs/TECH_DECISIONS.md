# Tech Decisions — StockSight

> Architecture Decision Records (ADRs). Each decision explains what was chosen, what alternatives were considered, and why.

---

## ADR-001: Blazor WebAssembly over React/Angular

**Decision:** Use Blazor WebAssembly for the frontend  
**Status:** Accepted

**Context:**  
Need a frontend framework. Developer has 1+ year C# experience but limited JavaScript/TypeScript experience.

**Options considered:**
| Option | Pros | Cons |
|---|---|---|
| React | Most popular, large ecosystem, great for hiring signal | Requires JS/TS, separate language from backend |
| Angular | Enterprise-ready, TypeScript | Steep learning curve, verbose |
| Vue | Gentle learning curve | Less enterprise adoption |
| Blazor WASM | Full C#, same models as backend, no context switching | Larger initial load, less job postings |

**Decision rationale:**  
Blazor WASM lets the developer write the entire stack in C# — sharing models like `StockTick` directly without DTO mapping between languages. For a portfolio project, this is a strong signal: "this developer can do full-stack C# with no JavaScript dependency." Recruiters who hire for .NET positions will find this impressive.

**Trade-off accepted:** Blazor WASM has a larger initial download (~10MB compressed) vs React. Mitigated with lazy loading and compression.

---

## ADR-002: SignalR over raw WebSockets

**Decision:** Use ASP.NET Core SignalR for real-time communication  
**Status:** Accepted

**Context:**  
Real-time stock price updates need a persistent connection from server to browser.

**Options considered:**
- Raw WebSocket (`System.Net.WebSockets`)
- SignalR (built-in ASP.NET abstraction)
- Server-Sent Events (SSE)
- Polling (simplest, worst UX)

**Decision rationale:**  
SignalR handles WebSocket connection management, fallback to long-polling, reconnection logic, and group broadcasting out of the box. Raw WebSockets would require implementing all of this manually. The `HubConnection` client in Blazor is first-class. SignalR groups map perfectly to the "subscribe per symbol" pattern.

---

## ADR-003: PostgreSQL + TimescaleDB over InfluxDB/MongoDB

**Decision:** PostgreSQL with TimescaleDB extension for time-series data  
**Status:** Accepted

**Context:**  
Need to store: user data (relational) and OHLCV bars (time-series, potentially millions of rows).

**Options considered:**
- PostgreSQL only — simple, familiar, but slow range queries on large time-series
- InfluxDB — purpose-built for time-series, but separate database means two connection strings, two ORMs
- MongoDB — document store, flexible but overkill, no TimescaleDB-style optimizations
- PostgreSQL + TimescaleDB — one database, relational tables for users/portfolios, hypertable for OHLCV

**Decision rationale:**  
TimescaleDB is a PostgreSQL extension. Same connection string, same EF Core provider. The `ohlcv_bars` table becomes a hypertable with automatic time partitioning, chunk compression, and fast range queries. Users, portfolios, and alerts stay as regular relational tables. Supabase offers PostgreSQL with TimescaleDB on free tier.

---

## ADR-004: Redis via StackExchange.Redis for caching

**Decision:** Redis for caching and pub/sub  
**Status:** Accepted

**Context:**  
Need to avoid hammering external APIs on every request (rate limits are low on free tiers). Also need a pub/sub mechanism for the ingestion service to notify the SignalR hub.

**Options considered:**
- In-memory cache (`IMemoryCache`) — single instance only, not scalable, lost on restart
- Distributed cache (`IDistributedCache` + SQL) — slow for sub-second tick data
- Redis — industry standard, O(1) reads, pub/sub built-in, Upstash free tier

**Decision rationale:**  
Redis `GET`/`SET` for cached quotes with TTL. Redis pub/sub channel `tick:{SYMBOL}` used by the ingestion service to broadcast ticks — the SignalR hub subscribes and forwards to clients. Upstash provides serverless Redis with free tier (10,000 commands/day, sufficient for demo).

---

## ADR-005: Yahoo Finance (unofficial) as primary data source

**Decision:** Use YahooFinanceApi NuGet package (unofficial) as the primary stock data provider  
**Status:** Accepted with caution

**Context:**  
Need stock price data. All professional data providers (Bloomberg, Refinitiv) are expensive. Free options have heavy rate limits.

**Options considered:**
| Provider | Cost | Rate Limit | Quality |
|---|---|---|---|
| Yahoo Finance (unofficial) | Free | High (unofficial) | Good for quotes + OHLCV |
| Alpha Vantage | Free | 25 req/day | Good for indicators |
| Polygon.io | Free | 5 calls/min, 2yr history | Good WebSocket |
| Finnhub | Free | 60 calls/min | Good |

**Decision rationale:**  
Yahoo Finance unofficial API has no key required and high rate limits, making it ideal for a demo that recruiters will click around in. `IStockDataProvider` interface means swapping to Polygon.io or Alpha Vantage requires zero changes to controllers or hub.

**Risk accepted:** Unofficial API could break without notice. Mitigation: graceful fallback to cached data, mock data service for presentation mode.

---

## ADR-006: JWT stored in Blazor memory state

**Decision:** Store JWT access token in Blazor memory (not localStorage, not sessionStorage)  
**Status:** Accepted

**Context:**  
Blazor WASM runs in the browser. Where to store auth tokens securely?

**Options:**
- `localStorage` — persists across tabs/sessions, but XSS can steal it
- `sessionStorage` — cleared on tab close, still XSS-vulnerable
- Cookie (httpOnly) — XSS-safe, but CSRF-vulnerable; requires SameSite config
- Memory state — XSS-safe (JS can't access .NET memory heap), lost on page refresh

**Decision rationale:**  
Memory state is the most secure option for Blazor WASM — JavaScript cannot access the .NET WebAssembly heap. The access token (1h expiry) lives in memory. The refresh token (7-day expiry) lives in an httpOnly, SameSite=Strict cookie set by the server. On page refresh, Blazor calls `POST /api/auth/refresh` automatically using the cookie to get a new access token. This is the recommended Microsoft pattern for Blazor WASM security.

---

## ADR-007: ML.NET for rule-based signals, OpenAI for sentiment

**Decision:** Hybrid AI signal approach — rule-based (ML.NET/manual) + LLM (OpenAI)  
**Status:** Accepted

**Context:**  
Need to generate trading signals. Options range from pure rule-based to full machine learning.

**Options considered:**
- Pure rule-based (RSI/MACD/Bollinger rules) — deterministic, fast, no API cost
- ML.NET classification model — requires training data, complex setup
- Pure OpenAI — expensive per call, not deterministic, no awareness of chart data
- Hybrid: rules for quantitative signals + OpenAI for qualitative sentiment

**Decision rationale:**  
Rule-based signals on technical indicators are deterministic and fast. OpenAI analyzes news headlines to add qualitative context. The combination (quantitative + qualitative) matches how real analysts work. OpenAI calls are cached for 1 hour — with 25 demo symbols checked once per hour, daily OpenAI cost is near $0 with the $5 free credit.

---

## ADR-008: Hangfire for scheduled jobs

**Decision:** Use Hangfire for background job scheduling  
**Status:** Accepted

**Context:**  
Need to run end-of-day jobs: aggregate intraday data into daily bars, clean up expired alerts, recalculate signals.

**Options considered:**
- `BackgroundService` with manual timer — already used for real-time ingestion, simple
- Hangfire — dashboard UI, retry on failure, persistent job queue
- Quartz.NET — powerful, more complex, heavyweight
- Azure Functions / AWS Lambda — overkill for this project

**Decision rationale:**  
`BackgroundService` is used for the real-time tick ingestion (runs continuously). Hangfire is added for scheduled (cron-based) jobs where persistence and retry matter: "if the EOD aggregation fails at midnight, retry at 12:05." Hangfire's built-in dashboard at `/hangfire` is also impressive to show in a demo. Uses PostgreSQL as its backing store (same DB, no extra infrastructure).

---

## ADR-009: Clean Architecture (not vertical slice)

**Decision:** Use Clean Architecture with separate projects (Core, Infrastructure, API, Web)  
**Status:** Accepted

**Context:**  
How to structure the solution?

**Options considered:**
- Monolith, single project — fast to start, messy at scale
- Vertical slice (feature folders) — great for teams, but less structured for a solo portfolio
- Clean Architecture (layer separation) — more files, but clear dependency rules, well-understood by enterprise .NET teams

**Decision rationale:**  
Clean Architecture is what enterprise .NET teams use. A recruiter opening the solution immediately recognizes the structure. The `Core` project having zero external dependencies is a strong signal of good design discipline. For this project's scope it adds some boilerplate (interfaces + implementations) but the payoff is a repo that reads like a real production codebase.

---

## ADR-010: Free-tier deployment stack

**Decision:** Railway (API) + Netlify (Blazor WASM) + Supabase (DB) + Upstash (Redis)  
**Status:** Accepted

| Service | Provider | Reason |
|---|---|---|
| API hosting | Railway.app | Supports Docker, .NET, free 500h/month |
| Frontend hosting | Netlify | Static site hosting, Blazor publish output is static |
| PostgreSQL | Supabase | Free tier, TimescaleDB extension available |
| Redis | Upstash | Serverless Redis, free 10k commands/day |
| Email | SendGrid | 100 emails/day free, no credit card |

**Total monthly cost: $0**
