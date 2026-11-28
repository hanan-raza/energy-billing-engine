using EnergyBillingEngine.Domain;
using Shouldly;

namespace EnergyBillingEngine.Tests.Unit.Domain;

public sealed class VatEngineTests
{
    [Fact]
    public void Known_net_total_gives_exact_vat_at_the_standard_rate()
    {
        // 497.66 × 19 % = 94.5554
        var vat = VatEngine.Calculate([Money.Eur(400.06m), Money.Eur(97.60m)], VatRate.Standard, Money.Euro);

        vat.Net.ShouldBe(Money.Eur(497.66m));
        vat.Vat.ShouldBe(Money.Eur(94.56m));
        vat.Gross.ShouldBe(Money.Eur(592.22m));
    }

    [Fact]
    public void The_reduced_rate_rounds_half_away_from_zero()
    {
        // 3.50 × 7 % = 0.245: commercial rounding gives 0.25, banker's rounding would give 0.24.
        var vat = VatEngine.Calculate([Money.Eur(3.50m)], VatRate.Reduced, Money.Euro);

        vat.Vat.ShouldBe(Money.Eur(0.25m));
        vat.Gross.ShouldBe(Money.Eur(3.75m));
    }

    [Fact]
    public void Vat_is_computed_once_on_the_total_not_per_line()
    {
        // Per line: 0.0152 → 0.02, three times 0.06. On the total: 0.24 × 19 % = 0.0456 → 0.05.
        var vat = VatEngine.Calculate([Money.Eur(0.08m), Money.Eur(0.08m), Money.Eur(0.08m)], VatRate.Standard, Money.Euro);

        vat.Vat.ShouldBe(Money.Eur(0.05m));
    }

    [Fact]
    public void A_credit_note_gets_negative_vat_rounded_symmetrically()
    {
        var vat = VatEngine.Calculate([Money.Eur(-3.50m)], VatRate.Reduced, Money.Euro);

        vat.Vat.ShouldBe(Money.Eur(-0.25m));
        vat.Gross.ShouldBe(Money.Eur(-3.75m));
    }

    [Fact]
    public void Levies_and_energy_together_give_the_invoice_totals()
    {
        // The levy lines of LevyCalculatorTests (497.60) plus 3,500 kWh × 0.324517 = 1135.8095 → 1135.81.
        var levies = LevyCalculator.Calculate(BillingPeriod.Year(2026), 3500m,
        [
            new(LevyType.NetworkFee, Money.Eur(0.0912m), new DateOnly(2026, 1, 1)),
            new(LevyType.ConcessionFee, Money.Eur(0.0166m), new DateOnly(2026, 1, 1)),
            new(LevyType.ElectricityTax, Money.Eur(0.0205m), new DateOnly(2026, 1, 1)),
            new(LevyType.ChpSurcharge, Money.Eur(0.00446m), new DateOnly(2026, 1, 1)),
            new(LevyType.OffshoreGridSurcharge, Money.Eur(0.00941m), new DateOnly(2026, 1, 1)),
        ]);
        var energy = Money.Eur(0.324517m).Multiply(3500m).RoundToCents();

        var vat = VatEngine.Calculate(levies.Select(l => l.Amount).Append(energy), VatRate.Standard, Money.Euro);

        // 1633.41 × 19 % = 310.3479
        vat.Net.ShouldBe(Money.Eur(1633.41m));
        vat.Vat.ShouldBe(Money.Eur(310.35m));
        vat.Gross.ShouldBe(Money.Eur(1943.76m));
    }

    [Fact]
    public void An_empty_invoice_has_zero_vat_in_its_currency()
    {
        var vat = VatEngine.Calculate([], VatRate.Standard, "CHF");

        vat.Net.ShouldBe(Money.Zero("CHF"));
        vat.Vat.ShouldBe(Money.Zero("CHF"));
    }

    [Fact]
    public void Unrounded_net_amounts_are_refused()
    {
        Should.Throw<ArgumentException>(() => VatEngine.Calculate([Money.Eur(1.005m)], VatRate.Standard, Money.Euro));
    }

    [Fact]
    public void Amounts_in_another_currency_are_refused()
    {
        Should.Throw<InvalidOperationException>(() => VatEngine.Calculate([new Money(1m, "CHF")], VatRate.Standard, Money.Euro));
    }
}

public sealed class VatRateTests
{
    [Theory]
    [InlineData("0")]
    [InlineData("7.5")]
    [InlineData("100")]
    public void Rates_from_0_to_100_percent_are_accepted(string percent)
    {
        new VatRate(decimal.Parse(percent, System.Globalization.CultureInfo.InvariantCulture)).Percent
            .ShouldBe(decimal.Parse(percent, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("100.01")]
    [InlineData("19.005")]
    public void Rates_outside_the_range_or_too_precise_are_refused(string percent)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new VatRate(decimal.Parse(percent, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void Rates_compare_by_value()
    {
        new VatRate(19.00m).ShouldBe(VatRate.Standard);
        VatRate.Reduced.ShouldNotBe(VatRate.Standard);
    }

    [Fact]
    public void Prints_as_percent()
    {
        VatRate.Standard.ToString().ShouldBe("19 %");
        new VatRate(7.5m).ToString().ShouldBe("7.5 %");
    }
}
