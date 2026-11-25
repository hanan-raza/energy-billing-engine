using EnergyBillingEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EnergyBillingEngine.Infrastructure;

/// <summary>Service registration for the infrastructure layer.</summary>
public static class DependencyInjection
{
    /// <summary>Registers the <see cref="BillingDbContext"/> on SQL Server.</summary>
    /// <exception cref="ArgumentException"><paramref name="connectionString"/> is empty.</exception>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<BillingDbContext>(options => options.UseBillingStore(connectionString));
        return services;
    }

    /// <summary>
    /// Single place for provider settings, so the app, the EF tools and the tests build the same model.
    /// The Oracle provider is added next to SQL Server here (Day 32).
    /// </summary>
    public static TBuilder UseBillingStore<TBuilder>(this TBuilder options, string connectionString)
        where TBuilder : DbContextOptionsBuilder
    {
        ArgumentNullException.ThrowIfNull(options);
        options.UseSqlServer(connectionString);
        return options;
    }
}
