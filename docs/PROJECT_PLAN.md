# Project Plan — StockSight

## 1. Project Summary

**Name:** StockSight  
**Type:** Personal portfolio project  
**Developer background:** 1+ year C# web developer  
**Purpose:** Demonstrate senior-level C# skills to recruiters via GitHub  
**Target completion:** ~10–12 weeks (part-time, solo)

---

## 2. Goals

### Primary Goals
- Build a visually impressive, technically deep full-stack C# application
- Showcase real-time systems (SignalR, WebSocket), not just CRUD
- Demonstrate AI integration (OpenAI, ML.NET) in a practical context
- Apply Clean Architecture so the codebase reads like a production system
- Deploy live with a public URL to share with recruiters

### Secondary Goals
- Learn TimescaleDB for time-series data
- Practice Docker and CI/CD (GitHub Actions)
- Write meaningful unit + integration tests

---

## 3. Target Audience (Recruiters)

This project is designed to impress:
- Vietnamese tech companies hiring .NET developers
- Startups looking for full-stack C# engineers
- Companies with fintech, data-heavy, or real-time products

**What they will see on GitHub:**
- A clean, well-documented repository
- Multiple NuGet packages and integrations
- SignalR real-time features (not common in junior portfolios)
- AI features (hot in 2024–2025 job market)
- Working live demo link

---

## 4. Scope

### In Scope
- Real-time stock price streaming (via free APIs)
- Candlestick + technical indicator charts
- AI-generated BUY/SELL/HOLD signals
- Strategy backtesting engine
- Virtual portfolio with paper trading
- Price alert system (in-app + email)
- News sentiment feed per ticker
- User authentication (JWT)
- Responsive Blazor WebAssembly frontend
- Docker Compose for local dev
- Deployment to free-tier cloud services
- Full documentation in `/docs`

### Out of Scope
- Real money trading or brokerage integration
- Mobile native app
- Multi-language (English only for codebase, Vietnamese for docs)
- Premium data feeds (all APIs are free tier)
- Admin panel

---

## 5. Non-Goals

- This is NOT a production trading platform
- This does NOT provide actual financial advice
- Performance optimization for millions of users is NOT a priority
- 100% test coverage is NOT required (focus on critical paths)

---

## 6. Success Criteria

The project is considered complete when:

- [ ] All Phase 1–4 features are implemented and working
- [ ] Live demo URL is publicly accessible
- [ ] README includes demo GIF/video
- [ ] At least 20 meaningful commits with clear messages
- [ ] At least 15 passing unit tests
- [ ] No secrets committed to the repository
- [ ] Lighthouse performance score > 70 on frontend
- [ ] Recruiter can run locally with 3 commands or less

---

## 7. Risks and Mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Free API rate limits hit during demo | High | Medium | Cache aggressively in Redis, use mock data fallback |
| Yahoo Finance API changes / breaks | Medium | High | Abstract behind IStockDataProvider interface, easy to swap |
| Scope creep (adding too many features) | High | Medium | Stick strictly to phase plan, log ideas in BACKLOG section of TODO.md |
| Blazor WASM load time too slow | Medium | Low | Enable compression, lazy load assemblies |
| TimescaleDB setup complexity | Low | Medium | Use plain PostgreSQL first, migrate later |

---

## 8. Timeline (Estimated)

| Phase | Focus | Duration |
|---|---|---|
| Phase 1 | Foundation & real-time core | Week 1–2 |
| Phase 2 | Charts & indicators | Week 3–4 |
| Phase 3 | AI signals & backtesting | Week 5–7 |
| Phase 4 | Portfolio, alerts, news | Week 8–9 |
| Phase 5 | Polish, tests, deploy | Week 10–12 |

See [PHASES.md](./PHASES.md) for detailed task breakdown per phase.

---

## 9. Tech Stack Rationale

Full justification in [TECH_DECISIONS.md](./TECH_DECISIONS.md). Summary:

- **C# / ASP.NET Core 8** — matches developer's existing skill, modern LTS version
- **Blazor WebAssembly** — full-stack C#, impressive to recruiters, no JS framework needed
- **SignalR** — built into ASP.NET, easiest real-time solution for C# developers
- **Redis** — industry standard for caching, free tier available
- **PostgreSQL + TimescaleDB** — production-grade, time-series optimized
- **ML.NET** — Microsoft's ML library, stays in C# ecosystem

---

## 10. External APIs Used (All Free)

| API | Purpose | Free Limit | Key Required |
|---|---|---|---|
| Yahoo Finance (unofficial) | OHLCV data, quotes | Unlimited (unofficial) | No |
| Alpha Vantage | Technical indicators, fundamentals | 25 req/day | Yes (free) |
| Polygon.io | WebSocket tick data | 5 years history | Yes (free) |
| NewsAPI | Financial news headlines | 100 req/day | Yes (free) |
| OpenAI API | Sentiment analysis | $5 free credit on signup | Yes |
| SendGrid | Email alerts | 100 emails/day | Yes (free) |
