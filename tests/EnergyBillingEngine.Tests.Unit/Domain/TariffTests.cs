using EnergyBillingEngine.Domain;
using Shouldly;

namespace EnergyBillingEngine.Tests.Unit.Domain;

public sealed class TariffTests
{
    private static Tariff Basic() => new("Basic", Money.Eur(120m), Money.Eur(0.30m));

    [Fact]
    public void Without_bands_every_time_of_day_has_the_energy_price()
    {
        var tariff = Basic();

        tariff.EnergyPriceAt(new TimeOnly(3, 0)).ShouldBe(Money.Eur(0.30m));
        tariff.EnergyPriceAt(new TimeOnly(18, 0)).ShouldBe(Money.Eur(0.30m));
    }

    [Theory]
    [InlineData(22, 0, "0.22")]
    [InlineData(23, 59, "0.22")]
    [InlineData(0, 0, "0.22")]
    [InlineData(5, 45, "0.22")]
    [InlineData(6, 0, "0.30")]
    [InlineData(21, 45, "0.30")]
    public void A_night_band_across_midnight_applies_from_its_start_until_before_its_end(int hour, int minute, string expected)
    {
        var tariff = Basic();
        tariff.AddTimeOfUseBand("NT", new TimeOnly(22, 0), new TimeOnly(6, 0), Money.Eur(0.22m));

        tariff.EnergyPriceAt(new TimeOnly(hour, minute))
            .ShouldBe(Money.Eur(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void Several_bands_each_get_their_own_price()
    {
        var tariff = Basic();
        tariff.AddTimeOfUseBand("NT", new TimeOnly(22, 0), new TimeOnly(6, 0), Money.Eur(0.22m));
        tariff.AddTimeOfUseBand("Peak", new TimeOnly(17, 0), new TimeOnly(20, 0), Money.Eur(0.40m));

        tariff.EnergyPriceAt(new TimeOnly(18, 30)).ShouldBe(Money.Eur(0.40m));
        tariff.EnergyPriceAt(new TimeOnly(23, 0)).ShouldBe(Money.Eur(0.22m));
        tariff.EnergyPriceAt(new TimeOnly(12, 0)).ShouldBe(Money.Eur(0.30m));
    }

    [Theory]
    [InlineData(5, 0, 7, 0)]
    [InlineData(21, 0, 23, 0)]
    [InlineData(23, 0, 1, 0)]
    [InlineData(20, 0, 8, 0)]
    public void Overlapping_bands_are_refused(int fromHour, int fromMinute, int toHour, int toMinute)
    {
        var tariff = Basic();
        tariff.AddTimeOfUseBand("NT", new TimeOnly(22, 0), new TimeOnly(6, 0), Money.Eur(0.22m));

        Should.Throw<ArgumentException>(() =>
            tariff.AddTimeOfUseBand("X", new TimeOnly(fromHour, fromMinute), new TimeOnly(toHour, toMinute), Money.Eur(0.25m)));
    }

    [Fact]
    public void Adjacent_bands_do_not_overlap()
    {
        var tariff = Basic();
        tariff.AddTimeOfUseBand("NT", new TimeOnly(22, 0), new TimeOnly(6, 0), Money.Eur(0.22m));

        Should.NotThrow(() => tariff.AddTimeOfUseBand("Morning", new TimeOnly(6, 0), new TimeOnly(9, 0), Money.Eur(0.32m)));
    }

    [Fact]
    public void An_empty_band_is_refused()
    {
        Should.Throw<ArgumentException>(() =>
            Basic().AddTimeOfUseBand("X", new TimeOnly(8, 0), new TimeOnly(8, 0), Money.Eur(0.25m)));
    }

    [Fact]
    public void A_band_in_another_currency_is_refused()
    {
        Should.Throw<ArgumentException>(() =>
            Basic().AddTimeOfUseBand("NT", new TimeOnly(22, 0), new TimeOnly(6, 0), new Money(0.22m, "CHF")));
    }

    [Fact]
    public void Base_and_energy_price_must_share_a_currency()
    {
        Should.Throw<ArgumentException>(() => new Tariff("Mixed", Money.Eur(120m), new Money(0.30m, "CHF")));
    }

    [Fact]
    public void Negative_prices_are_refused()
    {
        Should.Throw<ArgumentException>(() => new Tariff("Negative", Money.Eur(-1m), Money.Eur(0.30m)));
        Should.Throw<ArgumentException>(() => new Tariff("Negative", Money.Eur(120m), Money.Eur(-0.01m)));
    }

    [Fact]
    public void A_zero_base_price_is_allowed()
    {
        // Some products, e.g. dynamic tariffs, have no base price.
        Should.NotThrow(() => new Tariff("No base price", Money.Eur(0m), Money.Eur(0.30m)));
    }
}
