using EnergyBillingEngine.Domain;
using Shouldly;

namespace EnergyBillingEngine.Tests.Unit.Domain;

public sealed class LevyTests
{
    [Theory]
    [InlineData("2025-12-31", false)]
    [InlineData("2026-01-01", true)]
    [InlineData("2026-12-31", true)]
    [InlineData("2027-01-01", false)]
    public void A_rate_applies_from_its_first_to_its_last_day(string day, bool expected)
    {
        var levy = new Levy(LevyType.ElectricityTax, Money.Eur(0.0205m), new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));

        levy.IsValidOn(DateOnly.Parse(day, System.Globalization.CultureInfo.InvariantCulture)).ShouldBe(expected);
    }

    [Fact]
    public void An_open_ended_rate_applies_until_further_notice()
    {
        var levy = new Levy(LevyType.NetworkFee, Money.Eur(0.09m), new DateOnly(2026, 1, 1));

        levy.IsValidOn(new DateOnly(2040, 1, 1)).ShouldBeTrue();
    }

    [Fact]
    public void A_rate_cannot_end_before_it_starts()
    {
        Should.Throw<ArgumentException>(() =>
            new Levy(LevyType.ConcessionFee, Money.Eur(0.0166m), new DateOnly(2026, 1, 1), new DateOnly(2025, 12, 31)));
    }

    [Fact]
    public void A_zero_rate_is_allowed_for_abolished_surcharges()
    {
        Should.NotThrow(() => new Levy(LevyType.EegSurcharge, Money.Eur(0m), new DateOnly(2022, 7, 1)));
    }
}
