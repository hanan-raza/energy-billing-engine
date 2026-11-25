using EnergyBillingEngine.Domain;
using EnergyBillingEngine.Infrastructure;
using EnergyBillingEngine.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Shouldly;

namespace EnergyBillingEngine.Tests.Unit.Persistence;

/// <summary>
/// Checks the relational model without a database: building the model never opens a connection.
/// The decimal scales matter because SQL Server rounds silently to the column's scale.
/// </summary>
public sealed class BillingModelTests : IDisposable
{
    private readonly BillingDbContext _context = new(
        new DbContextOptionsBuilder<BillingDbContext>()
            .UseBillingStore("Server=model-only;Database=model-only")
            .Options);

    // The runtime model drops annotations such as column types; the design-time model keeps them.
    private IRelationalModel Model => _context.GetService<IDesignTimeModel>().Model.GetRelationalModel();

    public void Dispose() => _context.Dispose();

    [Fact]
    public void The_migrations_match_the_model()
    {
        // Fails when an entity or configuration changed without `dotnet ef migrations add`.
        _context.Database.HasPendingModelChanges().ShouldBeFalse();
    }

    [Theory]
    [InlineData("Tariffs", "BasePricePerYear")]
    [InlineData("Tariffs", "EnergyPricePerKwh")]
    [InlineData("TariffTimeOfUseBands", "EnergyPricePerKwh")]
    [InlineData("Levies", "RatePerKwh")]
    [InlineData("InvoiceLines", "UnitPrice")]
    public void Prices_keep_six_decimal_places(string table, string column)
    {
        Column(table, column).ShouldBe($"decimal(18,{Precision.PriceDecimals})");
    }

    [Fact]
    public void Line_amounts_are_stored_in_cents()
    {
        Column("InvoiceLines", "Amount").ShouldBe("decimal(18,2)");
    }

    [Fact]
    public void Quantities_keep_the_five_places_of_meter_data()
    {
        Column("InvoiceLines", "Quantity").ShouldBe("decimal(18,5)");
    }

    [Theory]
    [InlineData("Tariffs", "BasePricePerYearCurrency")]
    [InlineData("InvoiceLines", "AmountCurrency")]
    [InlineData("Invoices", "Currency")]
    public void Currencies_are_three_fixed_characters(string table, string column)
    {
        Column(table, column).ShouldBe("char(3)");
    }

    [Theory]
    [InlineData("Contracts", "StartDate")]
    [InlineData("Contracts", "EndDate")]
    [InlineData("Invoices", "PeriodStart")]
    [InlineData("Invoices", "PeriodEnd")]
    [InlineData("Levies", "ValidFrom")]
    public void Contract_and_billing_days_are_dates_not_instants(string table, string column)
    {
        Column(table, column).ShouldBe("date");
    }

    [Fact]
    public void The_market_location_id_is_exactly_eleven_characters()
    {
        Column("Contracts", "MarketLocationId").ShouldBe("char(11)");
    }

    [Theory]
    [InlineData("Levies", "Type")]
    [InlineData("InvoiceLines", "Type")]
    public void Enums_are_stored_as_readable_text(string table, string column)
    {
        Column(table, column).ShouldStartWith("varchar(");
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

    private string Column(string table, string column) =>
        Model.Tables.Single(t => t.Name == table).Columns.Single(c => c.Name == column).StoreType;
}
