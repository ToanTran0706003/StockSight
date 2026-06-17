using Microsoft.EntityFrameworkCore;
using StockSight.Core.Models;

namespace StockSight.Infrastructure.Data;

/// <summary>
/// EF Core database context backed by PostgreSQL (Npgsql).
/// </summary>
public class StockSightDbContext : DbContext
{
    public StockSightDbContext(DbContextOptions<StockSightDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<WatchlistItem> WatchlistItems => Set<WatchlistItem>();
    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<PortfolioHolding> Holdings => Set<PortfolioHolding>();
    public DbSet<PortfolioPosition> PortfolioPositions => Set<PortfolioPosition>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<PriceAlert> PriceAlerts => Set<PriceAlert>();
    public DbSet<OhlcvBar> OhlcvBars => Set<OhlcvBar>();
    public DbSet<TradeSignal> TradeSignals => Set<TradeSignal>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        var demoUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var seedTime = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Email).IsRequired().HasMaxLength(255);
            e.Property(u => u.PasswordHash).HasMaxLength(512);
            e.Property(u => u.DisplayName).HasMaxLength(120);
            e.HasIndex(u => u.Email).IsUnique();
            e.HasMany(u => u.WatchlistItems)
             .WithOne(w => w.User)
             .HasForeignKey(w => w.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(u => u.PriceAlerts)
             .WithOne(a => a.User)
             .HasForeignKey(a => a.UserId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(u => u.Portfolios)
             .WithOne()
             .HasForeignKey(p => p.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WatchlistItem>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.Symbol).IsRequired().HasMaxLength(16);
            e.HasIndex(w => new { w.UserId, w.Symbol }).IsUnique();
        });

        modelBuilder.Entity<Portfolio>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).IsRequired().HasMaxLength(120);
            e.Property(p => p.OwnerId).IsRequired().HasMaxLength(128);
            e.HasMany(p => p.Holdings)
             .WithOne(h => h.Portfolio!)
             .HasForeignKey(h => h.PortfolioId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(p => p.Positions)
             .WithOne(p => p.Portfolio!)
             .HasForeignKey(p => p.PortfolioId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PortfolioHolding>(e =>
        {
            e.HasKey(h => h.Id);
            e.Property(h => h.Symbol).IsRequired().HasMaxLength(16);
            e.Property(h => h.Quantity).HasPrecision(18, 4);
            e.Property(h => h.AverageCost).HasPrecision(18, 4);
        });

        modelBuilder.Entity<Alert>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Symbol).IsRequired().HasMaxLength(16);
            e.Property(a => a.OwnerId).IsRequired().HasMaxLength(128);
            e.Property(a => a.TargetPrice).HasPrecision(18, 4);
            e.Property(a => a.Condition).HasConversion<string>().HasMaxLength(16);
            e.Property(a => a.Status).HasConversion<string>().HasMaxLength(16);
            e.HasIndex(a => new { a.Symbol, a.Status });
        });

        modelBuilder.Entity<PortfolioPosition>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Symbol).IsRequired().HasMaxLength(16);
            e.Property(p => p.Shares).HasPrecision(18, 4);
            e.Property(p => p.AverageCost).HasPrecision(18, 4);
            e.HasIndex(p => new { p.PortfolioId, p.Symbol }).IsUnique();
        });

        modelBuilder.Entity<PriceAlert>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Symbol).IsRequired().HasMaxLength(16);
            e.Property(a => a.TargetPrice).HasPrecision(18, 4);
            e.Property(a => a.Direction).HasConversion<string>().HasMaxLength(16);
            e.HasIndex(a => new { a.UserId, a.Symbol });
        });

        modelBuilder.Entity<OhlcvBar>(e =>
        {
            e.HasKey(b => new { b.Symbol, b.TimestampUtc, b.Interval });
            e.Property(b => b.Symbol).HasMaxLength(16);
            e.Property(b => b.Interval).HasMaxLength(8);
            e.Property(b => b.Open).HasPrecision(18, 4);
            e.Property(b => b.High).HasPrecision(18, 4);
            e.Property(b => b.Low).HasPrecision(18, 4);
            e.Property(b => b.Close).HasPrecision(18, 4);
            e.HasIndex(b => new { b.Symbol, b.Interval, b.TimestampUtc });
        });

        modelBuilder.Entity<TradeSignal>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Symbol).IsRequired().HasMaxLength(16);
            e.Property(s => s.Action).HasConversion<string>().HasMaxLength(8);
            e.Property(s => s.Confidence).HasPrecision(5, 2);
            e.Property(s => s.SentimentScore).HasPrecision(5, 2);
        });

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = demoUserId,
            Email = "demo@stocksight.local",
            PasswordHash = "",
            DisplayName = "Demo User",
            CreatedUtc = seedTime
        });

        modelBuilder.Entity<WatchlistItem>().HasData(
            SeedWatchlistItem("aaaaaaaa-0000-0000-0000-000000000001", demoUserId, "AAPL", seedTime),
            SeedWatchlistItem("aaaaaaaa-0000-0000-0000-000000000002", demoUserId, "GOOGL", seedTime),
            SeedWatchlistItem("aaaaaaaa-0000-0000-0000-000000000003", demoUserId, "MSFT", seedTime),
            SeedWatchlistItem("aaaaaaaa-0000-0000-0000-000000000004", demoUserId, "TSLA", seedTime),
            SeedWatchlistItem("aaaaaaaa-0000-0000-0000-000000000005", demoUserId, "AMZN", seedTime));
    }

    private static WatchlistItem SeedWatchlistItem(string id, Guid userId, string symbol, DateTime addedUtc)
        => new()
        {
            Id = Guid.Parse(id),
            UserId = userId,
            Symbol = symbol,
            AddedUtc = addedUtc
        };
}
