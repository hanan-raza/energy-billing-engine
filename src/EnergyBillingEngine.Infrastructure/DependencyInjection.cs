using EnergyBillingEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Oracle.EntityFrameworkCore.Infrastructure;

namespace EnergyBillingEngine.Infrastructure;

/// <summary>Service registration for the infrastructure layer.</summary>
public static class DependencyInjection
{
    /// <summary>Registers the <see cref="BillingDbContext"/> on the given database.</summary>
    /// <exception cref="ArgumentException"><paramref name="connectionString"/> is empty.</exception>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, DatabaseProvider provider, string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<BillingDbContext>(options => options.UseBillingStore(provider, connectionString));
        return services;
    }

    /// <summary>
    /// Single place for provider settings, so the app, the EF tools and the tests build the same model.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="provider"/> is unknown.</exception>
    public static TBuilder UseBillingStore<TBuilder>(this TBuilder options, DatabaseProvider provider, string connectionString)
        where TBuilder : DbContextOptionsBuilder
    {
        ArgumentNullException.ThrowIfNull(options);

        // Migrations are provider-specific SQL; each provider reads them from its own assembly.
        var migrations = MigrationsAssembly(provider);
        _ = provider switch
        {
            DatabaseProvider.SqlServer => options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrations)),

            // Pinned to 23ai, the version of the compose image and the test container, so the generated
            // SQL does not depend on which server the tools happen to reach.
            DatabaseProvider.Oracle => options.UseOracle(connectionString, oracle => oracle
                .MigrationsAssembly(migrations)
                .UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion23)),

            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unknown database provider."),
        };
        return options;
    }

    /// <summary>Name of the assembly holding the migrations of <paramref name="provider"/>.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="provider"/> is unknown.</exception>
    public static string MigrationsAssembly(DatabaseProvider provider) => provider switch
    {
        DatabaseProvider.SqlServer => "EnergyBillingEngine.Migrations.SqlServer",
        DatabaseProvider.Oracle => "EnergyBillingEngine.Migrations.Oracle",
        _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unknown database provider."),
    };
}
