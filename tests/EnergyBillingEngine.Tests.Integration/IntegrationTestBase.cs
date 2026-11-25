using EnergyBillingEngine.Infrastructure;
using EnergyBillingEngine.Infrastructure.Persistence;

namespace EnergyBillingEngine.Tests.Integration;

/// <summary>
/// Base of the tests that run against a real database. Each test class is written once and runs once per
/// provider through a small derived class, so SQL Server and Oracle are held to the same expectations.
/// </summary>
/// <remarks>
/// The database is shared by all tests of a provider, so every test uses its own business keys
/// (see <see cref="UniqueKey"/>) instead of relying on an empty table.
/// </remarks>
public abstract class IntegrationTestBase(DatabaseFixture database)
{
    protected DatabaseProvider Provider => database.Provider;

    /// <summary>A fresh context with an empty change tracker, so reads really come from the database.</summary>
    protected BillingDbContext NewContext() => database.CreateContext();

    /// <summary>A business key no other test uses, within the 20 characters of customer, contract and invoice numbers.</summary>
    protected static string UniqueKey(string prefix) => prefix + Guid.NewGuid().ToString("N")[..12];
}
