using EnergyBillingEngine.Domain;
using Shouldly;

namespace EnergyBillingEngine.Tests.Unit.Domain;

public sealed class ContractTests
{
    private const string MaLo = "51238696781";
    private static readonly Tariff Basic = new("Basic", Money.Eur(120m), Money.Eur(0.30m));

    [Fact]
    public void Signing_adds_an_open_ended_contract_to_the_customer()
    {
        var customer = new Customer("K-1001", "Erika Mustermann");

        var contract = customer.SignContract("V-2001", MaLo, Basic, new DateOnly(2026, 3, 1));

        customer.Contracts.ShouldBe([contract]);
        contract.MarketLocationId.ShouldBe(MaLo);
        contract.Tariff.ShouldBeSameAs(Basic);
        contract.EndDate.ShouldBeNull();
    }

    [Theory]
    [InlineData("2026-02-28", false)]
    [InlineData("2026-03-01", true)]
    [InlineData("2026-12-31", true)]
    [InlineData("2027-01-01", false)]
    public void A_terminated_contract_is_active_from_start_to_last_day_inclusive(string day, bool expected)
    {
        var contract = new Customer("K-1001", "Erika Mustermann").SignContract("V-2001", MaLo, Basic, new DateOnly(2026, 3, 1));

        contract.Terminate(new DateOnly(2026, 12, 31));

        contract.IsActiveOn(DateOnly.Parse(day, System.Globalization.CultureInfo.InvariantCulture)).ShouldBe(expected);
    }

    [Fact]
    public void A_contract_cannot_end_before_it_starts()
    {
        var contract = new Customer("K-1001", "Erika Mustermann").SignContract("V-2001", MaLo, Basic, new DateOnly(2026, 3, 1));

        Should.Throw<ArgumentException>(() => contract.Terminate(new DateOnly(2026, 2, 28)));
    }

    [Fact]
    public void Supply_on_the_first_day_only_is_allowed()
    {
        var contract = new Customer("K-1001", "Erika Mustermann").SignContract("V-2001", MaLo, Basic, new DateOnly(2026, 3, 1));

        contract.Terminate(new DateOnly(2026, 3, 1));

        contract.IsActiveOn(new DateOnly(2026, 3, 1)).ShouldBeTrue();
    }

    [Fact]
    public void A_contract_cannot_be_terminated_twice()
    {
        var contract = new Customer("K-1001", "Erika Mustermann").SignContract("V-2001", MaLo, Basic, new DateOnly(2026, 3, 1));
        contract.Terminate(new DateOnly(2026, 12, 31));

        Should.Throw<InvalidOperationException>(() => contract.Terminate(new DateOnly(2027, 1, 31)));
    }

    [Theory]
    [InlineData("1234567890")]
    [InlineData("51238696782")]
    [InlineData("01238696781")]
    [InlineData("DE000123456")]
    public void An_invalid_market_location_id_is_refused(string maLo)
    {
        Should.Throw<ArgumentException>(() =>
            new Customer("K-1001", "Erika Mustermann").SignContract("V-2001", maLo, Basic, new DateOnly(2026, 3, 1)));
    }

    [Fact]
    public void The_same_market_location_cannot_be_supplied_twice_at_the_same_time()
    {
        var customer = new Customer("K-1001", "Erika Mustermann");
        customer.SignContract("V-2001", MaLo, Basic, new DateOnly(2026, 3, 1));

        Should.Throw<InvalidOperationException>(() => customer.SignContract("V-2002", MaLo, Basic, new DateOnly(2026, 6, 1)));
    }

    [Fact]
    public void A_follow_up_contract_may_start_the_day_after_the_previous_one_ends()
    {
        var customer = new Customer("K-1001", "Erika Mustermann");
        customer.SignContract("V-2001", MaLo, Basic, new DateOnly(2026, 3, 1)).Terminate(new DateOnly(2026, 12, 31));

        Should.NotThrow(() => customer.SignContract("V-2002", MaLo, Basic, new DateOnly(2027, 1, 1)));
        customer.Contracts.Count.ShouldBe(2);
    }

    [Fact]
    public void A_follow_up_contract_may_not_start_on_the_last_day_of_the_previous_one()
    {
        var customer = new Customer("K-1001", "Erika Mustermann");
        customer.SignContract("V-2001", MaLo, Basic, new DateOnly(2026, 3, 1)).Terminate(new DateOnly(2026, 12, 31));

        Should.Throw<InvalidOperationException>(() => customer.SignContract("V-2002", MaLo, Basic, new DateOnly(2026, 12, 31)));
    }

    [Theory]
    [InlineData("", "Name")]
    [InlineData("K-1", " ")]
    public void A_customer_needs_a_number_and_a_name(string number, string name)
    {
        Should.Throw<ArgumentException>(() => new Customer(number, name));
    }

    [Fact]
    public void Customer_text_is_trimmed()
    {
        var customer = new Customer("  K-1001 ", " Erika Mustermann ");

        customer.CustomerNumber.ShouldBe("K-1001");
        customer.Name.ShouldBe("Erika Mustermann");
    }
}
