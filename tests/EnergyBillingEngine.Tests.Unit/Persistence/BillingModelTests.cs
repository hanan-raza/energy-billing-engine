using EnergyBillingEngine.Domain;
using EnergyBillingEngine.Infrastructure;
using EnergyBillingEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Shouldly;

namespace EnergyBillingEngine.Tests.Unit.Persistence;

/// <summary>
/// Checks the relational model of each provider without a database: building the model never opens a connection.
/// The decimal scales matter because SQL Server and Oracle both round silently to the column's scale.
/// </summary>
public abstract class BillingModelTests : IDisposable
{
    private readonly BillingDbContext _context;

    protected BillingModelTests(DatabaseProvider provider, string connectionString)
    {
        _context = new BillingDbContext(
            new DbContextOptionsBuilder<BillingDbContext>().UseBillingStore(provider, connectionString).Options);
    }

    /// <summary>Store type of a decimal column with 18 digits and the given scale.</summary>
    protected abstract string DecimalType(int scale);

    /// <summary>Store type of a fixed-length, non-Unicode text column.</summary>
    protected abstract string FixedText(int length);

    /// <summary>Store type of a calendar day.</summary>
    protected abstract string DateType { get; }

    /// <summary>Start of the store type of a variable-length, non-Unicode text column.</summary>
    protected abstract string VariableTextPrefix { get; }

    // The runtime model drops annotations such as column types; the design-time model keeps them.
    private IRelationalModel Model => _context.GetService<IDesignTimeModel>().Model.GetRelationalModel();

    public void Dispose()
    {
        _context.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void The_migrations_match_the_model()
    {
        // Fails when an entity or configuration changed without `dotnet ef migrations add` for this provider.
        _context.Database.HasPendingModelChanges().ShouldBeFalse();
    }

    [Fact]
    public void Contracts_that_existed_before_the_vat_column_get_the_standard_rate()
    {
        // EF proposes 0 for a new non-null column, which would bill existing contracts without VAT.
        var migrations = _context.GetService<IMigrationsAssembly>();
        var type = migrations.Migrations.Single(m => m.Key.EndsWith("_AddContractVatRate", StringComparison.Ordinal)).Value;
        var migration = migrations.CreateMigration(type, _context.Database.ProviderName!);

        migration.UpOperations.OfType<AddColumnOperation>().Single(o => o.Name == "VatRatePercent")
            .DefaultValue.ShouldBe(VatRate.Standard.Percent);
    }

    [Fact]
    public void The_vat_rate_is_stored_as_a_percentage_with_two_decimals()
    {
        Column("Contracts", "VatRatePercent").ToUpperInvariant().ShouldBe("DECIMAL(5,2)");
    }

    [Theory]
    [InlineData("Tariffs", "BasePricePerYear")]
    [InlineData("Tariffs", "EnergyPricePerKwh")]
    [InlineData("TariffTimeOfUseBands", "EnergyPricePerKwh")]
    [InlineData("Levies", "RatePerKwh")]
    [InlineData("InvoiceLines", "UnitPrice")]
    public void Prices_keep_six_decimal_places(string table, string column)
    {
        Column(table, column).ShouldBe(DecimalType(Precision.PriceDecimals));
    }

    [Fact]
    public void Line_amounts_are_stored_in_cents()
    {
        Column("InvoiceLines", "Amount").ShouldBe(DecimalType(2));
    }

    [Fact]
    public void Quantities_keep_the_five_places_of_meter_data()
    {
        Column("InvoiceLines", "Quantity").ShouldBe(DecimalType(5));
    }

    [Theory]
    [InlineData("Tariffs", "BasePricePerYearCurrency")]
    [InlineData("InvoiceLines", "AmountCurrency")]
    [InlineData("Invoices", "Currency")]
    public void Currencies_are_three_fixed_characters(string table, string column)
    {
        Column(table, column).ShouldBe(FixedText(3));
    }

    [Theory]
    [InlineData("Contracts", "StartDate")]
    [InlineData("Contracts", "EndDate")]
    [InlineData("Invoices", "PeriodStart")]
    [InlineData("Invoices", "PeriodEnd")]
    [InlineData("Levies", "ValidFrom")]
    public void Contract_and_billing_days_are_dates_not_instants_or_text(string table, string column)
    {
        Column(table, column).ShouldBe(DateType);
    }

    [Fact]
    public void The_market_location_id_is_exactly_eleven_characters()
    {
        Column("Contracts", "MarketLocationId").ShouldBe(FixedText(11));
    }

    [Theory]
    [InlineData("Levies", "Type")]
    [InlineData("InvoiceLines", "Type")]
    public void Enums_are_stored_as_readable_text(string table, string column)
    {
        Column(table, column).ShouldStartWith(VariableTextPrefix);
    }

    [Theory]
    [InlineData("Customers", "CustomerNumber")]
    [InlineData("Contracts", "ContractNumber")]
    [InlineData("Invoices", "InvoiceNumber")]
    public void Business_keys_are_unique(string table, string column)
    {
        Model.Tables.Single(t => t.Name == table).Indexes
            .ShouldContain(i => i.IsUnique && i.Columns.Count == 1 && i.Columns[0].Name == column);
    }

    [Fact]
    public void Invoice_line_numbers_are_unique_per_invoice()
    {
        Model.Tables.Single(t => t.Name == "InvoiceLines").Indexes
            .ShouldContain(i => i.IsUnique && i.Columns.Count == 2 && i.Columns[0].Name == "InvoiceId" && i.Columns[1].Name == "Position");
    }

    protected string Column(string table, string column) =>
        Model.Tables.Single(t => t.Name == table).Columns.Single(c => c.Name == column).StoreType;
}

public sealed class SqlServerBillingModelTests() : BillingModelTests(DatabaseProvider.SqlServer, "Server=model-only;Database=model-only")
{
    protected override string DecimalType(int scale) => $"decimal(18,{scale})";

    protected override string FixedText(int length) => $"char({length})";

    protected override string DateType => "date";

    protected override string VariableTextPrefix => "varchar(";
}

public sealed class OracleBillingModelTests() : BillingModelTests(DatabaseProvider.Oracle, "User Id=model;Password=model;Data Source=model-only")
{
    protected override string DecimalType(int scale) => $"DECIMAL(18,{scale})";

    protected override string FixedText(int length) => $"CHAR({length})";

    protected override string DateType => "DATE";

    protected override string VariableTextPrefix => "VARCHAR2(";

    // Oracle has no TIME type; a time of day is an interval since midnight.
    [Theory]
    [InlineData("StartTime")]
    [InlineData("EndTime")]
    public void Band_times_are_intervals_not_text(string column)
    {
        Column("TariffTimeOfUseBands", column).ShouldBe("INTERVAL DAY(0) TO SECOND(0)");
    }
}
