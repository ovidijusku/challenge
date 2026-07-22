using Challenge.Core.Interfaces;
using Challenge.Infrastructure.Data;
using Challenge.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Challenge.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sql => sql.EnableRetryOnFailure()));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

        return services;
    }
}
