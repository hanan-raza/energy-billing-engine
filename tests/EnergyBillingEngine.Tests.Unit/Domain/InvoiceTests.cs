using EnergyBillingEngine.Domain;
using Shouldly;

namespace EnergyBillingEngine.Tests.Unit.Domain;

public sealed class InvoiceTests
{
    private static Contract MarchToDecember()
    {
        var tariff = new Tariff("Basic", Money.Eur(120m), Money.Eur(0.30m));
        var contract = new Customer("K-1001", "Erika Mustermann").SignContract("V-2001", "51238696781", tariff, new DateOnly(2026, 3, 1));
        contract.Terminate(new DateOnly(2026, 12, 31));
        return contract;
    }

    [Fact]
    public void A_line_amount_is_quantity_times_unit_price_rounded_to_cents()
    {
        var invoice = new Invoice("R-3001", MarchToDecember(), BillingPeriod.Month(2026, 3));

        var line = invoice.AddLine(InvoiceLineType.Energy, "Energy 1 to 31 March", 312.457m, "kWh", Money.Eur(0.324517m));

        // 312.457 × 0.324517 = 101.397608269 → 101.40
        line.Amount.ShouldBe(Money.Eur(101.40m));
        line.UnitPrice.ShouldBe(Money.Eur(0.324517m));
    }

    [Fact]
    public void Net_total_is_the_sum_of_the_rounded_lines()
    {
        var invoice = new Invoice("R-3001", MarchToDecember(), BillingPeriod.Month(2026, 3));
        invoice.AddLine(InvoiceLineType.BasePrice, "Base price 31 days", 31m, "days", Money.Eur(120m / 365m));
        invoice.AddLine(InvoiceLineType.Energy, "Energy", 312.457m, "kWh", Money.Eur(0.324517m));
        invoice.AddLine(InvoiceLineType.Levy, "Electricity tax", 312.457m, "kWh", Money.Eur(0.0205m));

        // 31 × 0.328767… = 10.19, 101.40, 312.457 × 0.0205 = 6.405… → 6.41
        invoice.NetTotal.ShouldBe(Money.Eur(10.19m + 101.40m + 6.41m));
        invoice.NetTotal.ShouldBe(Money.Sum(invoice.Lines.Select(l => l.Amount), Money.Euro));
    }

    [Fact]
    public void An_invoice_without_lines_totals_zero()
    {
        new Invoice("R-3001", MarchToDecember(), BillingPeriod.Month(2026, 3)).NetTotal.ShouldBe(Money.Eur(0m));
    }

    [Fact]
    public void A_price_in_another_currency_is_refused()
    {
        var invoice = new Invoice("R-3001", MarchToDecember(), BillingPeriod.Month(2026, 3));

        Should.Throw<ArgumentException>(() =>
            invoice.AddLine(InvoiceLineType.Energy, "Energy", 1m, "kWh", new Money(0.30m, "CHF")));
    }

    [Theory]
    [InlineData(2026, 2)]
    [InlineData(2027, 1)]
    public void A_period_outside_the_contract_is_refused(int year, int month)
    {
        Should.Throw<ArgumentException>(() => new Invoice("R-3001", MarchToDecember(), BillingPeriod.Month(year, month)));
    }

    [Fact]
    public void A_period_that_only_partly_overlaps_the_contract_is_allowed()
    {
        // The contract starts on 1 March; a February–March invoice bills the March days (pro rata, Day 33).
        Should.NotThrow(() =>
            new Invoice("R-3001", MarchToDecember(), new BillingPeriod(new DateOnly(2026, 2, 15), new DateOnly(2026, 3, 15))));
    }

    [Fact]
    public void Lines_keep_the_order_they_were_added_in()
    {
        var invoice = new Invoice("R-3001", MarchToDecember(), BillingPeriod.Month(2026, 3));
        invoice.AddLine(InvoiceLineType.Energy, "Energy", 1m, "kWh", Money.Eur(0.30m));
        invoice.AddLine(InvoiceLineType.BasePrice, "Base price", 1m, "days", Money.Eur(0.33m));

        invoice.Lines.Select(l => l.Type).ShouldBe([InvoiceLineType.Energy, InvoiceLineType.BasePrice]);
    }
}
