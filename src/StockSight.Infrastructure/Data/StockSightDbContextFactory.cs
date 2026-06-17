using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StockSight.Infrastructure.Data;

public class StockSightDbContextFactory : IDesignTimeDbContextFactory<StockSightDbContext>
{
    public StockSightDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<StockSightDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=stocksight;Username=postgres;Password=postgres")
            .Options;

        return new StockSightDbContext(options);
    }
}
