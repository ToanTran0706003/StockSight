# Database Schema — StockSight

## Technology
- **PostgreSQL 15** (via Supabase in production)
- **TimescaleDB extension** for `ohlcv_bars` time-series table
- **Entity Framework Core 8** with Npgsql provider
- Naming convention: `snake_case` for all tables and columns (using `EFCore.NamingConventions`)

---

## Entities

### users
| Column | Type | Constraints |
|---|---|---|
| id | uuid | PK, default gen_random_uuid() |
| email | varchar(255) | UNIQUE, NOT NULL |
| password_hash | varchar(512) | NOT NULL |
| display_name | varchar(100) | NOT NULL |
| created_at | timestamptz | NOT NULL, default now() |
| last_login_at | timestamptz | nullable |
| is_active | boolean | NOT NULL, default true |

---

### refresh_tokens
| Column | Type | Constraints |
|---|---|---|
| id | uuid | PK |
| user_id | uuid | FK → users.id, CASCADE DELETE |
| token | varchar(512) | NOT NULL, UNIQUE |
| expires_at | timestamptz | NOT NULL |
| created_at | timestamptz | NOT NULL, default now() |
| revoked_at | timestamptz | nullable |

---

### watchlist_items
| Column | Type | Constraints |
|---|---|---|
| id | uuid | PK |
| user_id | uuid | FK → users.id, CASCADE DELETE |
| symbol | varchar(20) | NOT NULL |
| added_at | timestamptz | NOT NULL, default now() |
| | | UNIQUE(user_id, symbol) |

---

### portfolios
| Column | Type | Constraints |
|---|---|---|
| id | uuid | PK |
| user_id | uuid | FK → users.id, CASCADE DELETE |
| name | varchar(100) | NOT NULL |
| initial_cash | decimal(18,2) | NOT NULL |
| available_cash | decimal(18,2) | NOT NULL |
| created_at | timestamptz | NOT NULL, default now() |

---

### portfolio_positions
| Column | Type | Constraints |
|---|---|---|
| id | uuid | PK |
| portfolio_id | uuid | FK → portfolios.id, CASCADE DELETE |
| symbol | varchar(20) | NOT NULL |
| shares | decimal(18,4) | NOT NULL |
| average_cost | decimal(18,4) | NOT NULL |
| | | UNIQUE(portfolio_id, symbol) |

---

### portfolio_transactions
| Column | Type | Constraints |
|---|---|---|
| id | uuid | PK |
| portfolio_id | uuid | FK → portfolios.id |
| symbol | varchar(20) | NOT NULL |
| action | varchar(4) | NOT NULL — 'BUY' or 'SELL' |
| shares | decimal(18,4) | NOT NULL |
| price_per_share | decimal(18,4) | NOT NULL |
| total_value | decimal(18,2) | NOT NULL |
| executed_at | timestamptz | NOT NULL, default now() |

---

### price_alerts
| Column | Type | Constraints |
|---|---|---|
| id | uuid | PK |
| user_id | uuid | FK → users.id, CASCADE DELETE |
| symbol | varchar(20) | NOT NULL |
| target_price | decimal(18,4) | NOT NULL |
| direction | varchar(10) | NOT NULL — 'ABOVE' or 'BELOW' |
| is_active | boolean | NOT NULL, default true |
| is_triggered | boolean | NOT NULL, default false |
| triggered_at | timestamptz | nullable |
| created_at | timestamptz | NOT NULL, default now() |
| notify_email | boolean | NOT NULL, default true |
| notify_push | boolean | NOT NULL, default true |

---

### ohlcv_bars (TimescaleDB hypertable)
| Column | Type | Constraints |
|---|---|---|
| symbol | varchar(20) | NOT NULL |
| timestamp | timestamptz | NOT NULL |
| interval | varchar(5) | NOT NULL — '1m', '5m', '1h', '1d' |
| open | decimal(18,4) | NOT NULL |
| high | decimal(18,4) | NOT NULL |
| low | decimal(18,4) | NOT NULL |
| close | decimal(18,4) | NOT NULL |
| volume | bigint | NOT NULL |
| | | PRIMARY KEY (symbol, timestamp, interval) |

```sql
-- Convert to TimescaleDB hypertable (run once after creating table)
SELECT create_hypertable('ohlcv_bars', 'timestamp');

-- Index for fast retrieval
CREATE INDEX ON ohlcv_bars (symbol, interval, timestamp DESC);
```

---

### trade_signals (optional — for signal history)
| Column | Type | Constraints |
|---|---|---|
| id | uuid | PK |
| symbol | varchar(20) | NOT NULL |
| action | varchar(4) | NOT NULL — 'BUY', 'SELL', 'HOLD' |
| confidence | decimal(5,2) | NOT NULL — 0.00 to 100.00 |
| reason | text | nullable |
| sentiment_score | decimal(5,2) | nullable |
| generated_at | timestamptz | NOT NULL, default now() |

---

## C# Entity Models

### StockTick.cs
```csharp
public class StockTick
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public long Volume { get; set; }
    public decimal DayHigh { get; set; }
    public decimal DayLow { get; set; }
    public decimal Open { get; set; }
    public decimal PreviousClose { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### OhlcvBar.cs
```csharp
public class OhlcvBar
{
    public string Symbol { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Interval { get; set; } = string.Empty;
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public long Volume { get; set; }
}
```

### User.cs
```csharp
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<WatchlistItem> WatchlistItems { get; set; } = [];
    public ICollection<Portfolio> Portfolios { get; set; } = [];
    public ICollection<PriceAlert> PriceAlerts { get; set; } = [];
}
```

### Portfolio.cs
```csharp
public class Portfolio
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal InitialCash { get; set; }
    public decimal AvailableCash { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<PortfolioPosition> Positions { get; set; } = [];
    public ICollection<PortfolioTransaction> Transactions { get; set; } = [];
}
```

### PriceAlert.cs
```csharp
public class PriceAlert
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public decimal TargetPrice { get; set; }
    public AlertDirection Direction { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsTriggered { get; set; } = false;
    public DateTime? TriggeredAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool NotifyEmail { get; set; } = true;
    public bool NotifyPush { get; set; } = true;

    public User User { get; set; } = null!;
}

public enum AlertDirection { Above, Below }
```

### TradeSignal.cs
```csharp
public class TradeSignal
{
    public string Symbol { get; set; } = string.Empty;
    public SignalAction Action { get; set; }
    public decimal Confidence { get; set; }    // 0–100
    public string Reason { get; set; } = string.Empty;
    public decimal? SentimentScore { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public enum SignalAction { Buy, Sell, Hold }
```

---

## Migrations

```bash
# Create first migration
dotnet ef migrations add InitialCreate --project StockSight.Infrastructure --startup-project StockSight.API

# Apply to database
dotnet ef database update --project StockSight.Infrastructure --startup-project StockSight.API

# Add new migration (example)
dotnet ef migrations add AddTradeSignalsTable --project StockSight.Infrastructure --startup-project StockSight.API
```

---

## Seed Data

Default seed in `StockSightDbContext.OnModelCreating`:
```csharp
// 5 popular symbols pre-configured for demo
modelBuilder.Entity<WatchlistItem>().HasData(
    new WatchlistItem { Id = ..., UserId = demoUserId, Symbol = "AAPL" },
    new WatchlistItem { Id = ..., UserId = demoUserId, Symbol = "GOOGL" },
    new WatchlistItem { Id = ..., UserId = demoUserId, Symbol = "MSFT" },
    new WatchlistItem { Id = ..., UserId = demoUserId, Symbol = "TSLA" },
    new WatchlistItem { Id = ..., UserId = demoUserId, Symbol = "AMZN" }
);
```
