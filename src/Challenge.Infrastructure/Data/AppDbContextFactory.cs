using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Challenge.Infrastructure.Data;

/// <summary>
/// Enables `dotnet ef migrations` to construct the context at design time without
/// running the API host. Uses a local connection string only for tooling.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
            ?? "Server=localhost,1433;Database=ChallengeDb;User Id=sa;Password=Id0ntKn0WC#2WellButItsN0tFirstRode0;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
