using EnergyBillingEngine.Domain;
using Shouldly;

namespace EnergyBillingEngine.Tests.Unit.Domain;

public sealed class BillingPeriodTests
{
    [Fact]
    public void A_single_day_period_has_one_day()
    {
        new BillingPeriod(new DateOnly(2026, 3, 15), new DateOnly(2026, 3, 15)).Days.ShouldBe(1);
    }

    [Theory]
    [InlineData(2026, 2, 28)]
    [InlineData(2028, 2, 29)]
    [InlineData(2026, 3, 31)]
    [InlineData(2026, 4, 30)]
    public void Month_covers_every_day_of_the_month_including_leap_days(int year, int month, int expectedDays)
    {
        var period = BillingPeriod.Month(year, month);

        period.Start.ShouldBe(new DateOnly(year, month, 1));
        period.Days.ShouldBe(expectedDays);
    }

    [Theory]
    [InlineData(2026, 365)]
    [InlineData(2028, 366)]
    public void Year_has_365_or_366_days(int year, int expectedDays)
    {
        BillingPeriod.Year(year).Days.ShouldBe(expectedDays);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(10)]
    public void A_month_with_a_clock_change_still_counts_calendar_days(int month)
    {
        // The 23-hour (29 March) and 25-hour (25 October) days are one day each for pro-rata prices.
        BillingPeriod.Month(2026, month).Days.ShouldBe(31);
    }

    [Fact]
    public void End_before_start_is_refused()
    {
        Should.Throw<ArgumentException>(() => new BillingPeriod(new DateOnly(2026, 3, 2), new DateOnly(2026, 3, 1)));
    }

    [Theory]
    [InlineData("2026-03-01", true)]
    [InlineData("2026-03-31", true)]
    [InlineData("2026-02-28", false)]
    [InlineData("2026-04-01", false)]
    public void Contains_includes_both_ends(string day, bool expected)
    {
        BillingPeriod.Month(2026, 3).Contains(DateOnly.Parse(day, System.Globalization.CultureInfo.InvariantCulture)).ShouldBe(expected);
    }
}
