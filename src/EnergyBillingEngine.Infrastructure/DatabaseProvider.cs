namespace EnergyBillingEngine.Infrastructure;

/// <summary>The relational database the billing store runs on.</summary>
/// <remarks>Selected with the configuration key <c>BillingEngine:Database</c>.</remarks>
public enum DatabaseProvider
{
    /// <summary>Microsoft SQL Server 2022.</summary>
    SqlServer,

    /// <summary>Oracle Database 23ai (Free in development and tests).</summary>
    Oracle,
}
