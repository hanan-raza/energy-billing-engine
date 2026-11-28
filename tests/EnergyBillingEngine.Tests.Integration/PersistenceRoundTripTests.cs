using EnergyBillingEngine.Domain;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace EnergyBillingEngine.Tests.Integration;

/// <summary>
/// Writes domain objects with one context and reads them back with another. A wrong column type shows up
/// here and not in the unit tests: a truncated decimal, a lost time of day, a date that no longer compares.
/// </summary>
public abstract class PersistenceRoundTripTests(DatabaseFixture database) : IntegrationTestBase(database)
{
    [Fact]
    public async Task A_customer_reads_back_unchanged()
    {
        var customer = new Customer(UniqueKey("K-"), "Erika Mustermann");
        await using (var write = NewContext())
        {
            write.Customers.Add(customer);
            await write.SaveChangesAsync();
        }

        await using var read = NewContext();
        var loaded = await read.Customers.SingleAsync(c => c.CustomerNumber == customer.CustomerNumber);

        loaded.ShouldNotBeSameAs(customer);
        loaded.Id.ShouldNotBe(Guid.Empty);
        loaded.Id.ShouldBe(customer.Id);
        loaded.CustomerNumber.ShouldBe(customer.CustomerNumber);
        loaded.Name.ShouldBe("Erika Mustermann");
    }

    [Fact]
    public async Task Non_ascii_names_survive()
    {
        // Names are Unicode columns (nvarchar / NVARCHAR2); a non-Unicode column would turn these into '?'.
        var customer = new Customer(UniqueKey("K-"), "Jürgen Weiß-Ørsted");
        await using (var write = NewContext())
        {
            write.Customers.Add(customer);
            await write.SaveChangesAsync();
        }

        await using var read = NewContext();
        (await read.Customers.SingleAsync(c => c.Id == customer.Id)).Name.ShouldBe("Jürgen Weiß-Ørsted");
    }

    [Fact]
    public async Task A_customer_number_can_only_be_used_once()
    {
        var number = UniqueKey("K-");
        await using (var first = NewContext())
        {
            first.Customers.Add(new Customer(number, "First"));
            await first.SaveChangesAsync();
        }

        await using var second = NewContext();
        second.Customers.Add(new Customer(number, "Second"));
        await Should.ThrowAsync<DbUpdateException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task An_invoice_with_contract_and_tariff_reads_back_unchanged()
    {
        var tariff = new Tariff("Night saver", Money.Eur(120m), Money.Eur(0.324517m));
        tariff.AddTimeOfUseBand("NT", new TimeOnly(22, 0), new TimeOnly(6, 0), Money.Eur(0.22m));
        var customer = new Customer(UniqueKey("K-"), "Erika Mustermann");
        var contract = customer.SignContract(UniqueKey("V-"), "51238696781", tariff, new DateOnly(2026, 3, 1), VatRate.Reduced);
        var invoice = new Invoice(UniqueKey("R-"), contract, BillingPeriod.Month(2026, 3));
        invoice.AddLine(InvoiceLineType.Energy, "Energy", 312.457m, "kWh", Money.Eur(0.324517m));
        invoice.AddLine(InvoiceLineType.BasePrice, "Base price", 31m, "days", Money.Eur(0.328767m));

        await using (var write = NewContext())
        {
            write.AddRange(tariff, customer, invoice);
            await write.SaveChangesAsync();
        }

        await using var read = NewContext();
        var loaded = await read.Invoices
            .Include(i => i.Lines)
            .Include(i => i.Contract).ThenInclude(c => c.Tariff).ThenInclude(t => t.TimeOfUseBands)
            .SingleAsync(i => i.Id == invoice.Id);

        loaded.Period.ShouldBe(BillingPeriod.Month(2026, 3));
        loaded.Contract.StartDate.ShouldBe(new DateOnly(2026, 3, 1));
        loaded.Contract.EndDate.ShouldBeNull();
        loaded.Contract.MarketLocationId.ShouldBe("51238696781");
        loaded.Contract.VatRate.ShouldBe(VatRate.Reduced);

        // All six price decimals come back; a smaller scale would change the line amounts.
        loaded.Lines.Select(l => (l.Position, l.Quantity, l.UnitPrice, l.Amount)).ShouldBe(
        [
            (1, 312.457m, Money.Eur(0.324517m), Money.Eur(101.40m)),
            (2, 31m, Money.Eur(0.328767m), Money.Eur(10.19m)),
        ]);
        loaded.NetTotal.ShouldBe(Money.Eur(111.59m));

        // The night band crosses midnight; both ends must come back as times of day.
        var band = loaded.Contract.Tariff.TimeOfUseBands.ShouldHaveSingleItem();
        (band.From, band.To).ShouldBe((new TimeOnly(22, 0), new TimeOnly(6, 0)));
        loaded.Contract.Tariff.EnergyPriceAt(new TimeOnly(23, 0)).ShouldBe(Money.Eur(0.22m));
        loaded.Contract.Tariff.EnergyPriceAt(new TimeOnly(12, 0)).ShouldBe(Money.Eur(0.324517m));
    }

    [Fact]
    public async Task The_contract_active_on_a_day_is_found_by_the_database()
    {
        // The date comparison runs as SQL on the server, including the open end of a running contract.
        var tariff = new Tariff("Fixed", Money.Eur(120m), Money.Eur(0.30m));
        var customer = new Customer(UniqueKey("K-"), "Max Mustermann");
        var march = customer.SignContract(UniqueKey("V-"), "41373559241", tariff, new DateOnly(2026, 3, 1));
        march.Terminate(new DateOnly(2026, 3, 31));
        var april = customer.SignContract(UniqueKey("V-"), "41373559241", tariff, new DateOnly(2026, 4, 1));

        await using (var write = NewContext())
        {
            write.AddRange(tariff, customer);
            await write.SaveChangesAsync();
        }

        await using var read = NewContext();
        var numberOn = async (DateOnly day) => (await read.Contracts
            .Where(c => c.CustomerId == customer.Id && c.StartDate <= day && (c.EndDate == null || c.EndDate >= day))
            .SingleAsync()).ContractNumber;

        (await numberOn(new DateOnly(2026, 3, 31))).ShouldBe(march.ContractNumber);
        (await numberOn(new DateOnly(2026, 4, 1))).ShouldBe(april.ContractNumber);
    }

    [Fact]
    public async Task The_largest_price_the_columns_allow_survives()
    {
        // decimal(18,6): 12 integer digits and 6 decimals, the edge of what the domain accepts.
        var levy = new Levy(LevyType.NetworkFee, Money.Eur(999_999_999_999.999999m), new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
        await using (var write = NewContext())
        {
            write.Levies.Add(levy);
            await write.SaveChangesAsync();
        }

        await using var read = NewContext();
        var loaded = await read.Levies.SingleAsync(l => l.Id == levy.Id);
        loaded.RatePerKwh.ShouldBe(Money.Eur(999_999_999_999.999999m));
        (loaded.ValidFrom, loaded.ValidTo).ShouldBe((new DateOnly(2026, 1, 1), (DateOnly?)new DateOnly(2026, 12, 31)));
    }
}

[Collection(SqlServerTests.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "SqlServer")]
public sealed class SqlServerPersistenceRoundTripTests(SqlServerDatabase database) : PersistenceRoundTripTests(database);

[Collection(OracleTests.Name)]
[Trait("Category", "Integration")]
[Trait("Database", "Oracle")]
public sealed class OraclePersistenceRoundTripTests(OracleDatabase database) : PersistenceRoundTripTests(database);
