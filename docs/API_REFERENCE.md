# API Reference — StockSight

**Base URL (dev):** `https://localhost:7080/api`
**Base URL (prod):** `https://stocksight-api.railway.app/api`  
**Auth:** Bearer JWT token in `Authorization` header (required on marked endpoints)  
**Format:** All requests and responses are `application/json`

---

## Authentication

### POST /api/auth/register
Register a new user account.

**Request body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePass123!",
  "displayName": "Nguyen Van A"
}
```

**Response 201:**
```json
{
  "userId": "uuid",
  "email": "user@example.com",
  "displayName": "Nguyen Van A"
}
```

**Errors:** 400 (validation), 409 (email already exists)

---

### POST /api/auth/login
Login and receive JWT token.

**Request body:**
```json
{
  "email": "user@example.com",
  "password": "SecurePass123!"
}
```

**Response 200:**
```json
{
  "accessToken": "eyJhbGci...",
  "expiresIn": 3600,
  "userId": "uuid",
  "displayName": "Nguyen Van A"
}
```
_Refresh token set as httpOnly cookie automatically._

**Errors:** 401 (invalid credentials), 400 (validation)

---

### POST /api/auth/refresh
Exchange refresh token cookie for new access token.

**Response 200:**
```json
{
  "accessToken": "eyJhbGci...",
  "expiresIn": 3600
}
```

---

### POST /api/auth/logout
🔒 Auth required. Revoke refresh token.

**Response 204:** No content.

---

## Stocks

### GET /api/stocks/{symbol}/quote
Get current real-time quote for a symbol.

**Response 200:**
```json
{
  "symbol": "AAPL",
  "price": 182.45,
  "change": 1.23,
  "changePercent": 0.68,
  "volume": 54821000,
  "dayHigh": 183.10,
  "dayLow": 180.90,
  "open": 181.20,
  "previousClose": 181.22,
  "timestamp": "2024-11-15T20:00:00Z"
}
```

**Errors:** 404 (symbol not found)

---

### GET /api/stocks/{symbol}/ohlcv
Get historical OHLCV bars.

**Query parameters:**
| Param | Type | Required | Default | Options |
|---|---|---|---|---|
| interval | string | No | 1d | 1m, 5m, 15m, 1h, 4h, 1d, 1w |
| from | datetime | No | 30 days ago | ISO 8601 |
| to | datetime | No | now | ISO 8601 |

**Response 200:**
```json
{
  "symbol": "AAPL",
  "interval": "1d",
  "bars": [
    {
      "timestamp": "2024-11-14T00:00:00Z",
      "open": 180.10,
      "high": 183.50,
      "low": 179.80,
      "close": 181.22,
      "volume": 61234000
    }
  ]
}
```

---

### GET /api/stocks/{symbol}/info
Get fundamental stock information.

**Response 200:**
```json
{
  "symbol": "AAPL",
  "companyName": "Apple Inc.",
  "sector": "Technology",
  "industry": "Consumer Electronics",
  "marketCap": 2850000000000,
  "peRatio": 29.4,
  "eps": 6.11,
  "dividendYield": 0.52,
  "fiftyTwoWeekHigh": 198.23,
  "fiftyTwoWeekLow": 143.90,
  "averageVolume": 58000000,
  "description": "Apple Inc. designs, manufactures..."
}
```

---

### GET /api/stocks/search
Search for stock symbols by keyword.

**Query parameters:**
| Param | Type | Required |
|---|---|---|
| q | string | Yes (min 1 char) |

**Response 200:**
```json
{
  "results": [
    { "symbol": "AAPL", "name": "Apple Inc.", "exchange": "NASDAQ" },
    { "symbol": "AAPLX", "name": "...", "exchange": "NYSE" }
  ]
}
```

---

## Technical Indicators

All indicator endpoints accept optional `period` parameter where applicable.

### GET /api/stocks/{symbol}/indicators/rsi
**Query:** `?period=14`  
**Response:**
```json
{
  "symbol": "AAPL",
  "indicator": "RSI",
  "period": 14,
  "values": [
    { "timestamp": "2024-11-14T00:00:00Z", "value": 58.4 }
  ],
  "currentValue": 58.4,
  "signal": "Neutral"
}
```

### GET /api/stocks/{symbol}/indicators/macd
**Query:** `?fast=12&slow=26&signal=9`  
**Response:**
```json
{
  "macdLine": [...],
  "signalLine": [...],
  "histogram": [...],
  "currentMacd": 1.23,
  "currentSignal": 0.98,
  "crossover": "Bullish"
}
```

### GET /api/stocks/{symbol}/indicators/bollinger
**Query:** `?period=20&stdDev=2`

### GET /api/stocks/{symbol}/indicators/sma
**Query:** `?period=20`

### GET /api/stocks/{symbol}/indicators/ema
**Query:** `?period=50`

---

## Signals

### GET /api/stocks/{symbol}/signal
Get AI-generated trading signal for a symbol.

**Response 200:**
```json
{
  "id": "uuid",
  "symbol": "AAPL",
  "action": "Buy",
  "confidence": 72.5,
  "reason": "RSI oversold (28.3), MACD bullish crossover, positive sentiment from recent earnings news.",
  "sentimentScore": 0.68,
  "generatedUtc": "2024-11-15T14:30:00Z"
}
```

Available actions: `Buy`, `Sell`, `Hold`

---

## Backtesting

### POST /api/backtest
Run a backtest simulation.

**Request body:**
```json
{
  "symbol": "AAPL",
  "strategy": "SmaCrossover",
  "from": "2023-01-01",
  "to": "2024-01-01",
  "initialCapital": 10000.00
}
```

Available strategies: `SmaCrossover`, `RsiReversal`, `Macd`

**Response 200:**
```json
{
  "symbol": "AAPL",
  "strategy": "SmaCrossover",
  "from": "2023-01-01",
  "to": "2024-01-01",
  "initialCapital": 10000.00,
  "finalValue": 13420.50,
  "totalReturnPercent": 34.2,
  "sharpeRatio": 1.42,
  "maxDrawdownPercent": -8.3,
  "winRate": 62.5,
  "totalTrades": 16,
  "winningTrades": 10,
  "losingTrades": 6,
  "equityCurve": [
    { "date": "2023-01-01", "value": 10000.00 },
    { "date": "2023-01-15", "value": 10340.00 }
  ],
  "trades": [
    {
      "date": "2023-01-15",
      "action": "BUY",
      "price": 130.40,
      "shares": 10,
      "value": 1304.00
    }
  ]
}
```

---

## Portfolio

### GET /api/portfolio
🔒 Auth required. Get all user portfolios.

**Response 200:**
```json
{
  "portfolios": [
    {
      "id": "uuid",
      "name": "My Portfolio",
      "initialCash": 10000.00,
      "availableCash": 4230.50,
      "currentValue": 12450.80,
      "totalPnL": 2450.80,
      "totalPnLPercent": 24.5,
      "positionCount": 3
    }
  ]
}
```

### POST /api/portfolio
🔒 Auth required. Create a new portfolio.

**Request body:**
```json
{
  "name": "Growth Portfolio",
  "initialCash": 10000.00
}
```

### GET /api/portfolio/{id}
🔒 Auth required. Get portfolio detail with positions and P&L.

**Response 200:**
```json
{
  "id": "uuid",
  "name": "My Portfolio",
  "availableCash": 4230.50,
  "positions": [
    {
      "symbol": "AAPL",
      "shares": 10,
      "averageCost": 175.20,
      "currentPrice": 182.45,
      "marketValue": 1824.50,
      "unrealizedPnL": 72.50,
      "unrealizedPnLPercent": 4.14,
      "weight": 15.4
    }
  ],
  "totalValue": 12450.80,
  "totalPnL": 2450.80,
  "totalPnLPercent": 24.5
}
```

### POST /api/portfolio/{id}/buy
🔒 Auth required.

**Request body:**
```json
{
  "symbol": "AAPL",
  "shares": 5
}
```

**Response 200:**
```json
{
  "symbol": "AAPL",
  "shares": 5,
  "pricePerShare": 182.45,
  "totalCost": 912.25,
  "remainingCash": 3318.25
}
```

**Errors:** 400 (insufficient cash), 404 (symbol not found)

### POST /api/portfolio/{id}/sell
🔒 Auth required.

**Request body:**
```json
{
  "symbol": "AAPL",
  "shares": 3
}
```

**Errors:** 400 (insufficient shares)

---

## Alerts

### GET /api/alerts
🔒 Auth required.

**Response 200:**
```json
{
  "alerts": [
    {
      "id": "uuid",
      "symbol": "AAPL",
      "targetPrice": 190.00,
      "direction": "Above",
      "isActive": true,
      "isTriggered": false,
      "createdAt": "2024-11-01T10:00:00Z"
    }
  ]
}
```

### POST /api/alerts
🔒 Auth required.

**Request body:**
```json
{
  "symbol": "AAPL",
  "targetPrice": 190.00,
  "direction": "Above",
  "notifyEmail": true,
  "notifyPush": true
}
```

### DELETE /api/alerts/{id}
🔒 Auth required. **Response 204.**

---

## News

### GET /api/news/{symbol}
Get recent news with sentiment analysis for a symbol.

**Query:** `?limit=10`

**Response 200:**
```json
{
  "symbol": "AAPL",
  "articles": [
    {
      "title": "Apple reports record Q4 earnings",
      "source": "Reuters",
      "publishedAt": "2024-11-01T18:00:00Z",
      "url": "https://...",
      "sentiment": "Bullish",
      "sentimentScore": 0.82,
      "sentimentReason": "Strong earnings beat with record iPhone sales mentioned."
    }
  ]
}
```

---

## Watchlist

### GET /api/watchlist
🔒 Auth required.

### POST /api/watchlist/{symbol}
🔒 Auth required. Add symbol. **Response 201.**

### DELETE /api/watchlist/{symbol}
🔒 Auth required. **Response 204.**

---

## Health

### GET /health
Public endpoint. Returns system status.

**Response 200:**
```json
{
  "status": "Healthy",
  "checks": {
    "database": "Healthy",
    "redis": "Healthy",
    "yahooFinance": "Healthy"
  },
  "version": "1.0.0",
  "timestamp": "2024-11-15T14:00:00Z"
}
```

---

## SignalR Hub

**URL:** `/hubs/stock`  
**Protocol:** WebSocket (with long-polling fallback)

### Client → Server methods
| Method | Params | Description |
|---|---|---|
| `SubscribeToSymbol` | `string symbol` | Join real-time price group |
| `UnsubscribeFromSymbol` | `string symbol` | Leave price group |

### Server → Client events
| Event | Payload | Description |
|---|---|---|
| `ReceiveTick` | `StockTick` | New price update |
| `ReceiveSignal` | `TradeSignal` | New AI signal |
| `AlertTriggered` | `PriceAlert` | Alert condition met |

### Connection example (Blazor)
```csharp
var hub = new HubConnectionBuilder()
    .WithUrl("/hubs/stock")
    .WithAutomaticReconnect()
    .Build();

hub.On<StockTick>("ReceiveTick", tick => {
    // Update UI
});

await hub.StartAsync();
await hub.InvokeAsync("SubscribeToSymbol", "AAPL");
```
