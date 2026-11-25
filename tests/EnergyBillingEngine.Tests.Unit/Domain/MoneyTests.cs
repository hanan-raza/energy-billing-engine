using EnergyBillingEngine.Domain;
using Shouldly;

namespace EnergyBillingEngine.Tests.Unit.Domain;

public sealed class MoneyTests
{
    [Fact]
    public void Adding_amounts_in_the_same_currency_adds_exactly()
    {
        // 0.1 + 0.2 is the classic double trap (0.30000000000000004); decimal must be exact.
        (Money.Eur(0.1m) + Money.Eur(0.2m)).ShouldBe(Money.Eur(0.3m));
    }

    [Fact]
    public void Subtracting_can_go_negative_for_credits()
    {
        (Money.Eur(10.00m) - Money.Eur(12.50m)).ShouldBe(Money.Eur(-2.50m));
    }

    [Fact]
    public void Negating_flips_the_sign_and_keeps_the_currency()
    {
        (-new Money(12.34m, "CHF")).ShouldBe(new Money(-12.34m, "CHF"));
    }

    [Theory]
    [InlineData("EUR", "CHF")]
    [InlineData("CHF", "EUR")]
    public void Combining_different_currencies_is_refused(string left, string right)
    {
        var a = new Money(1m, left);
        var b = new Money(1m, right);

        Should.Throw<InvalidOperationException>(() => a + b);
        Should.Throw<InvalidOperationException>(() => a - b);
    }

    [Theory]
    [InlineData("eur")]
    [InlineData("EURO")]
    [InlineData("E1R")]
    [InlineData("")]
    [InlineData(null)]
    public void Currency_must_be_three_upper_case_letters(string? currency)
    {
        Should.Throw<ArgumentException>(() => new Money(1m, currency!));
    }

    [Fact]
    public void Multiplying_a_price_by_a_quantity_is_not_rounded()
    {
        // 0.324517 EUR/kWh × 1234.567 kWh: rounding the price first would lose cents over a year.
        var amount = Money.Eur(0.324517m) * 1234.567m;

        amount.ShouldBe(Money.Eur(400.637979139m));
        (1234.567m * Money.Eur(0.324517m)).ShouldBe(amount);
    }

    [Theory]
    [InlineData("0.125", "0.13")]
    [InlineData("0.135", "0.14")]
    [InlineData("2.5049", "2.50")]
    [InlineData("-0.125", "-0.13")]
    public void Rounding_to_cents_is_commercial_rounding(string amount, string expected)
    {
        Money.Eur(Parse(amount)).RoundToCents().ShouldBe(Money.Eur(Parse(expected)));
    }

    [Theory]
    [InlineData("0.125", "0.12")]
    [InlineData("0.135", "0.14")]
    [InlineData("-0.125", "-0.12")]
    public void Bankers_rounding_is_available_explicitly_and_rounds_half_to_even(string amount, string expected)
    {
        Money.Eur(Parse(amount)).Round(2, MidpointRounding.ToEven).ShouldBe(Money.Eur(Parse(expected)));
    }

    [Fact]
    public void Rounding_each_line_and_summing_can_differ_from_rounding_the_sum()
    {
        // Three lines of 0.005 EUR: each rounds to 0.01, the sum 0.015 rounds to 0.02. Invoices round per
        // line (InvoiceLine), so the printed lines always add up to the printed total.
        var lines = Enumerable.Repeat(Money.Eur(0.005m), 3).ToList();

        Money.Sum(lines.Select(l => l.RoundToCents()), Money.Euro).ShouldBe(Money.Eur(0.03m));
        Money.Sum(lines, Money.Euro).RoundToCents().ShouldBe(Money.Eur(0.02m));
    }

    [Fact]
    public void Sum_of_nothing_is_zero_in_the_given_currency()
    {
        Money.Sum([], "CHF").ShouldBe(Money.Zero("CHF"));
    }

    [Fact]
    public void Sum_refuses_mixed_currencies()
    {
        Should.Throw<InvalidOperationException>(() => Money.Sum([Money.Eur(1m), new Money(1m, "CHF")], Money.Euro));
    }

    [Fact]
    public void Equal_amount_and_currency_are_equal_values()
    {
        Money.Eur(1.50m).ShouldBe(new Money(1.50m, "EUR"));
        Money.Eur(1.50m).ShouldNotBe(new Money(1.50m, "CHF"));
    }

    [Fact]
    public void ToString_does_not_depend_on_the_current_culture()
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            Money.Eur(1234.5m).ToString().ShouldBe("1234.5 EUR");
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    private static decimal Parse(string value) => decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
}
