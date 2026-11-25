using DotNet.Testcontainers.Containers;
using EnergyBillingEngine.Infrastructure;
using EnergyBillingEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;
using Testcontainers.Oracle;

namespace EnergyBillingEngine.Tests.Integration;

/// <summary>
/// One database container per provider, started once for all test classes of its collection and
/// migrated with the provider's own migrations, the same way the application would deploy them.
/// </summary>
public abstract class DatabaseFixture : IAsyncLifetime
{
    // Same images as docker-compose.yml, so tests and local development hit the same server versions.
    private const string SqlServerImage = "mcr.microsoft.com/mssql/server:2022-latest";
    private const string OracleImage = "gvenzl/oracle-free:23-slim";

    private readonly IDatabaseContainer _container;

    protected DatabaseFixture(DatabaseProvider provider)
    {
        Provider = provider;
        _container = provider switch
        {
            DatabaseProvider.SqlServer => new MsSqlBuilder(SqlServerImage).Build(),
            DatabaseProvider.Oracle => new OracleBuilder(OracleImage).Build(),
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, "Unknown database provider."),
        };
    }

    public DatabaseProvider Provider { get; }

    public BillingDbContext CreateContext() => new(
        new DbContextOptionsBuilder<BillingDbContext>()
            .UseBillingStore(Provider, _container.GetConnectionString())
            .Options);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}

public sealed class SqlServerDatabase() : DatabaseFixture(DatabaseProvider.SqlServer);

public sealed class OracleDatabase() : DatabaseFixture(DatabaseProvider.Oracle);

[CollectionDefinition(Name)]
public sealed class SqlServerTests : ICollectionFixture<SqlServerDatabase>
{
    public const string Name = "SQL Server";
}

[CollectionDefinition(Name)]
public sealed class OracleTests : ICollectionFixture<OracleDatabase>
{
    public const string Name = "Oracle";
}
