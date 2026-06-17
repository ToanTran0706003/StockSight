# StockSight — Real-time Stock Dashboard

> A full-stack, AI-powered stock market dashboard built with ASP.NET Core 8, Blazor WebAssembly, and SignalR. Designed as a portfolio project to demonstrate enterprise-grade C# development skills.

---

## Overview

StockSight is a real-time stock market dashboard that streams live price data, generates AI-driven trading signals, supports strategy backtesting, and simulates virtual portfolios — all in one platform.

**Developer:** [Your Name]  
**Stack:** C# / ASP.NET Core 8 / Blazor WASM / SignalR / Redis / PostgreSQL  
**Status:** 🚧 In Development  
**Live Demo:** _coming soon_  
**Repo:** https://github.com/[username]/StockSight

---

## Why This Project

The Vietnamese and global IT market is increasingly competitive. This project was built to demonstrate:

- Real-time systems design (WebSocket / SignalR)
- Clean Architecture in a production-like codebase
- AI/ML integration (signal generation, sentiment analysis)
- Full-stack C# (same language front-to-back with Blazor)
- DevOps basics: Docker, CI/CD, cloud deployment

---

## Key Features

| Feature | Description | Tech |
|---|---|---|
| Live candlestick charts | Tick-by-tick OHLCV updates | SignalR + TradingView Charts |
| AI trading signals | BUY/SELL/HOLD with confidence score | ML.NET + OpenAI |
| Strategy backtesting | Historical simulation with metrics | TimescaleDB + C# engine |
| Price alerts | Threshold-based push notifications | Redis pub/sub + SendGrid |
| Virtual portfolio | Paper trading with real prices | EF Core + real-time P&L |
| News sentiment | Article analysis per ticker | NewsAPI + OpenAI |

---

## Architecture Summary

```
[Data Sources]          [Backend]                [Storage]
Yahoo Finance API  -->  Data Ingestion Service
Alpha Vantage      -->  ASP.NET Core 8 API   -->  PostgreSQL + TimescaleDB
Polygon.io WS      -->  SignalR Hub          -->  Redis Cache
NewsAPI            -->  AI Signal Engine     -->  (Upstash free tier)
                            |
                        [Frontend]
                    Blazor WebAssembly SPA
                    TradingView Lightweight Charts
                    SignalR Client (real-time)
```

---

## Project Structure

```
StockSight/
├── docs/                          ← All documentation lives here
│   ├── README.md                  ← This file
│   ├── PROJECT_PLAN.md            ← Vision, goals, scope
│   ├── ARCHITECTURE.md            ← System design, diagrams
│   ├── PHASES.md                  ← Roadmap broken into phases
│   ├── TODO.md                    ← Active task tracker
│   ├── API_REFERENCE.md           ← Endpoint documentation
│   ├── DATABASE_SCHEMA.md         ← Entity models and ERD
│   ├── TECH_DECISIONS.md          ← Why each technology was chosen
│   ├── DEPLOYMENT.md              ← Deploy guide (free tier)
│   └── CONTRIBUTING.md            ← Coding conventions, git flow
├── src/
│   ├── StockSight.API/            ← ASP.NET Core 8 Web API
│   ├── StockSight.Core/           ← Domain models, interfaces
│   ├── StockSight.Infrastructure/ ← Redis, DB, external APIs
│   └── StockSight.Web/            ← Blazor WebAssembly frontend
├── tests/
│   └── StockSight.Tests/          ← xUnit test project
├── docker-compose.yml
├── .github/workflows/ci.yml
└── StockSight.sln
```

---

## Quick Start (Local Development)

### Prerequisites
- .NET 8 SDK
- Docker Desktop
- Git

### Steps

```bash
# 1. Clone repo
git clone https://github.com/[username]/StockSight.git
cd StockSight

# 2. Start infrastructure (Redis + PostgreSQL)
docker-compose up -d

# 3. Apply database migrations
cd src/StockSight.API
dotnet ef database update

# 4. Run API
dotnet run --project src/StockSight.API

# 5. Run Blazor frontend (separate terminal)
dotnet run --project src/StockSight.Web
```

### Environment Variables (appsettings.Development.json)
```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Database=stocksight;Username=postgres;Password=postgres",
    "Redis": "localhost:6379"
  },
  "ApiKeys": {
    "AlphaVantage": "YOUR_KEY",
    "NewsApi": "YOUR_KEY",
    "OpenAI": "YOUR_KEY",
    "SendGrid": "YOUR_KEY"
  }
}
```

---

## Free Tier Deployment

| Service | Provider | Cost |
|---|---|---|
| API hosting | Railway.app | Free |
| Blazor frontend | Netlify | Free |
| PostgreSQL | Supabase | Free |
| Redis | Upstash | Free |
| Email | SendGrid | Free (100/day) |

---

## Docs Index

- [Project Plan](./PROJECT_PLAN.md) — goals, scope, non-goals
- [Architecture](./ARCHITECTURE.md) — system design decisions
- [Phases & Roadmap](./PHASES.md) — what gets built when
- [TODO](./TODO.md) — current sprint tasks
- [API Reference](./API_REFERENCE.md) — all endpoints
- [Database Schema](./DATABASE_SCHEMA.md) — models and relationships
- [Tech Decisions](./TECH_DECISIONS.md) — ADRs (Architecture Decision Records)
- [Deployment Guide](./DEPLOYMENT.md) — free-tier deploy steps
- [Contributing](./CONTRIBUTING.md) — coding standards
