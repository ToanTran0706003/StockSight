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

    public DbSet<Portfolio> Portfolios => Set<Portfolio>();
    public DbSet<PortfolioHolding> Holdings => Set<PortfolioHolding>();
    public DbSet<Alert> Alerts => Set<Alert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Portfolio>(e =>
        {
            e.HasKey(p => p.Id);
            e.Property(p => p.Name).IsRequired().HasMaxLength(120);
            e.Property(p => p.OwnerId).IsRequired().HasMaxLength(128);
            e.HasMany(p => p.Holdings)
             .WithOne(h => h.Portfolio!)
             .HasForeignKey(h => h.PortfolioId)
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
    }
}
