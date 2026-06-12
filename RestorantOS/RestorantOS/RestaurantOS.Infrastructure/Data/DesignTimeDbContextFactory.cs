using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RestaurantOS.Infrastructure.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost,14330;Database=RestaurantOS;User Id=sa;Password=RestaurantOS@2024!;TrustServerCertificate=True;Encrypt=False;")
            .Options;

        return new AppDbContext(options);
    }
}
