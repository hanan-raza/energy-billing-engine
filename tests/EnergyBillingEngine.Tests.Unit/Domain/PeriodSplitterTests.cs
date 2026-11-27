using EnergyBillingEngine.Domain;
using Shouldly;

namespace EnergyBillingEngine.Tests.Unit.Domain;

public sealed class PeriodSplitterTests
{
    private static readonly Tariff Old = new("Old", Money.Eur(120m), Money.Eur(0.30m));
    private static readonly Tariff New = new("New", Money.Eur(132m), Money.Eur(0.34m));
    private static readonly Tariff Newer = new("Newer", Money.Eur(144m), Money.Eur(0.36m));

    [Fact]
    public void A_period_without_changes_is_one_part_with_the_tariff_in_effect()
    {
        var parts = PeriodSplitter.Split(BillingPeriod.Month(2026, 1), [new(new DateOnly(2025, 6, 1), Old)]);

        var part = parts.ShouldHaveSingleItem();
        part.Period.ShouldBe(BillingPeriod.Month(2026, 1));
        part.Tariff.ShouldBeSameAs(Old);
        part.Days.ShouldBe(31);
    }

    [Fact]
    public void A_price_change_on_the_15th_splits_the_month_into_days_1_to_14_and_15_to_31()
    {
        var parts = PeriodSplitter.Split(
            BillingPeriod.Month(2026, 1),
            [new(new DateOnly(2025, 6, 1), Old), new(new DateOnly(2026, 1, 15), New)]);

        parts.Select(p => (p.Period.Start.Day, p.Period.End.Day, p.Days, p.Tariff.Name)).ShouldBe(
        [
            (1, 14, 14, "Old"),
            (15, 31, 17, "New"),
        ]);
    }

    [Fact]
    public void Several_changes_give_one_part_each()
    {
        var parts = PeriodSplitter.Split(
            BillingPeriod.Year(2026),
            [new(new DateOnly(2025, 1, 1), Old), new(new DateOnly(2026, 4, 1), New), new(new DateOnly(2026, 10, 1), Newer)]);

        parts.Select(p => (p.Period, p.Tariff.Name)).ShouldBe(
        [
            (new BillingPeriod(new DateOnly(2026, 1, 1), new DateOnly(2026, 3, 31)), "Old"),
            (new BillingPeriod(new DateOnly(2026, 4, 1), new DateOnly(2026, 9, 30)), "New"),
            (new BillingPeriod(new DateOnly(2026, 10, 1), new DateOnly(2026, 12, 31)), "Newer"),
        ]);
        parts.Sum(p => p.Days).ShouldBe(365);
    }

    [Fact]
    public void March_with_the_spring_clock_change_has_31_days_but_only_743_hours()
    {
        var part = PeriodSplitter.Split(BillingPeriod.Month(2026, 3), [new(new DateOnly(2026, 1, 1), Old)]).ShouldHaveSingleItem();

        // Pro rata counts calendar days. Dividing the real hours by 24 would bill 30.96 days.
        part.Days.ShouldBe(31);
        part.Duration.ShouldBe(TimeSpan.FromHours(31 * 24 - 1));
        part.Start.ShouldBe(new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.FromHours(1)));
        part.End.ShouldBe(new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.FromHours(2)));
    }

    [Fact]
    public void October_with_the_autumn_clock_change_has_31_days_and_745_hours()
    {
        var part = PeriodSplitter.Split(BillingPeriod.Month(2026, 10), [new(new DateOnly(2026, 1, 1), Old)]).ShouldHaveSingleItem();

        part.Days.ShouldBe(31);
        part.Duration.ShouldBe(TimeSpan.FromHours(31 * 24 + 1));
        part.Start.Offset.ShouldBe(TimeSpan.FromHours(2));
        part.End.Offset.ShouldBe(TimeSpan.FromHours(1));
    }

    [Fact]
    public void A_change_on_the_day_the_clocks_go_forward_starts_at_local_midnight()
    {
        // 29 March 2026 has 23 hours. The new tariff starts at 00:00 local time (still +01:00), not at 03:00.
        var parts = PeriodSplitter.Split(
            BillingPeriod.Month(2026, 3),
            [new(new DateOnly(2026, 1, 1), Old), new(new DateOnly(2026, 3, 29), New)]);

        parts.Select(p => (p.Days, p.Duration)).ShouldBe(
        [
            (28, TimeSpan.FromHours(28 * 24)),
            (3, TimeSpan.FromHours(3 * 24 - 1)),
        ]);
        parts[0].End.ShouldBe(parts[1].Start);
        parts[1].Start.ShouldBe(new DateTimeOffset(2026, 3, 29, 0, 0, 0, TimeSpan.FromHours(1)));
    }

    [Fact]
    public void A_change_on_the_first_day_replaces_the_old_tariff_for_the_whole_period()
    {
        var parts = PeriodSplitter.Split(
            BillingPeriod.Month(2026, 2),
            [new(new DateOnly(2025, 1, 1), Old), new(new DateOnly(2026, 2, 1), New)]);

        parts.ShouldHaveSingleItem().Tariff.ShouldBeSameAs(New);
    }

    [Fact]
    public void A_change_on_the_last_day_gives_a_one_day_part()
    {
        var parts = PeriodSplitter.Split(
            BillingPeriod.Month(2028, 2),
            [new(new DateOnly(2028, 1, 1), Old), new(new DateOnly(2028, 2, 29), New)]);

        parts.Select(p => (p.Days, p.Tariff.Name)).ShouldBe([(28, "Old"), (1, "New")]);
    }

    [Fact]
    public void Changes_after_the_period_are_ignored()
    {
        var parts = PeriodSplitter.Split(
            BillingPeriod.Month(2026, 1),
            [new(new DateOnly(2025, 1, 1), Old), new(new DateOnly(2026, 2, 1), New)]);

        parts.ShouldHaveSingleItem().Tariff.ShouldBeSameAs(Old);
    }

    [Fact]
    public void The_order_of_the_changes_does_not_matter()
    {
        TariffChange[] history = [new(new DateOnly(2026, 1, 1), Old), new(new DateOnly(2026, 5, 10), New), new(new DateOnly(2026, 8, 20), Newer)];

        var sorted = PeriodSplitter.Split(BillingPeriod.Year(2026), history);
        var shuffled = PeriodSplitter.Split(BillingPeriod.Year(2026), [history[2], history[0], history[1]]);

        shuffled.ShouldBe(sorted);
    }

    [Fact]
    public void A_period_that_starts_before_the_first_tariff_is_refused()
    {
        // These days have no agreed price; a contract that starts mid-period needs a shorter period.
        var ex = Should.Throw<InvalidOperationException>(() => PeriodSplitter.Split(
            BillingPeriod.Month(2026, 3),
            [new(new DateOnly(2026, 3, 10), Old)]));

        ex.Message.ShouldContain("2026-03-01");
    }

    [Fact]
    public void Two_tariffs_on_the_same_day_are_refused()
    {
        Should.Throw<ArgumentException>(() => PeriodSplitter.Split(
            BillingPeriod.Month(2026, 3),
            [new(new DateOnly(2026, 1, 1), Old), new(new DateOnly(2026, 3, 15), New), new(new DateOnly(2026, 3, 15), Newer)]));
    }

    [Fact]
    public void Parts_cover_every_day_exactly_once_with_the_tariff_in_effect_that_day()
    {
        // Checks the splitter against the obvious day-by-day lookup for many periods and histories.
        var random = new Random(32);
        Tariff[] tariffs = [Old, New, Newer];
        for (var run = 0; run < 500; run++)
        {
            var start = new DateOnly(2026, 1, 1).AddDays(random.Next(0, 365));
            var period = new BillingPeriod(start, start.AddDays(random.Next(0, 400)));
            var history = Enumerable.Range(0, random.Next(0, 6))
                .Select(_ => period.Start.AddDays(random.Next(1, period.Days + 30)))
                .Distinct()
                .Select((day, i) => new TariffChange(day, tariffs[i % 3]))
                .Append(new TariffChange(period.Start.AddDays(-random.Next(0, 100)), Old))
                .ToList();

            var parts = PeriodSplitter.Split(period, history);

            parts[0].Period.Start.ShouldBe(period.Start);
            parts[^1].Period.End.ShouldBe(period.End);
            parts.Sum(p => p.Days).ShouldBe(period.Days);
            for (var i = 1; i < parts.Count; i++)
            {
                parts[i].Period.Start.ShouldBe(parts[i - 1].Period.End.AddDays(1));
                parts[i].Start.ShouldBe(parts[i - 1].End);
            }

            foreach (var part in parts)
            {
                for (var day = part.Period.Start; day <= part.Period.End; day = day.AddDays(1))
                {
                    var expected = history.Where(c => c.EffectiveFrom <= day).MaxBy(c => c.EffectiveFrom)!.Tariff;
                    part.Tariff.ShouldBeSameAs(expected);
                }
            }
        }
    }
}
