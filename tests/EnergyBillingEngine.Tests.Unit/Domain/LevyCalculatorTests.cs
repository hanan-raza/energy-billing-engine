using EnergyBillingEngine.Domain;
using Shouldly;

namespace EnergyBillingEngine.Tests.Unit.Domain;

public sealed class LevyCalculatorTests
{
    private static readonly DateOnly Jan1 = new(2026, 1, 1);

    // Synthetic 2026 rates; only the structure follows the German market.
    private static readonly Levy[] Rates2026 =
    [
        new(LevyType.NetworkFee, Money.Eur(0.0912m), Jan1),
        new(LevyType.ConcessionFee, Money.Eur(0.0166m), Jan1),
        new(LevyType.ElectricityTax, Money.Eur(0.0205m), Jan1),
        new(LevyType.ChpSurcharge, Money.Eur(0.00446m), Jan1),
        new(LevyType.OffshoreGridSurcharge, Money.Eur(0.00941m), Jan1),
        new(LevyType.EegSurcharge, Money.Eur(0m), new DateOnly(2022, 7, 1)),
    ];

    [Fact]
    public void Known_consumption_gives_exact_itemised_amounts()
    {
        var charges = LevyCalculator.Calculate(BillingPeriod.Year(2026), 3500m, Rates2026);

        charges.Select(c => (c.Type, c.QuantityKwh, c.Amount)).ShouldBe(
        [
            (LevyType.NetworkFee, 3500m, Money.Eur(319.20m)),
            (LevyType.ConcessionFee, 3500m, Money.Eur(58.10m)),
            (LevyType.ElectricityTax, 3500m, Money.Eur(71.75m)),
            (LevyType.ChpSurcharge, 3500m, Money.Eur(15.61m)),
            (LevyType.OffshoreGridSurcharge, 3500m, Money.Eur(32.94m)),
        ]);
        Money.Sum(charges.Select(c => c.Amount), Money.Euro).ShouldBe(Money.Eur(497.60m));
    }

    [Fact]
    public void Amounts_round_half_away_from_zero()
    {
        // 2,500 kWh × 0.00941 = 23.525: commercial rounding gives 23.53, banker's rounding would give 23.52.
        var charge = LevyCalculator.Calculate(BillingPeriod.Year(2026), 2500m, [new(LevyType.OffshoreGridSurcharge, Money.Eur(0.00941m), Jan1)])
            .ShouldHaveSingleItem();

        charge.Amount.ShouldBe(Money.Eur(23.53m));
    }

    [Fact]
    public void A_zero_rate_prints_no_line()
    {
        LevyCalculator.Calculate(BillingPeriod.Year(2026), 3500m, Rates2026)
            .ShouldNotContain(c => c.Type == LevyType.EegSurcharge);
    }

    [Fact]
    public void Types_without_any_rate_are_not_charged()
    {
        LevyCalculator.Calculate(BillingPeriod.Year(2026), 3500m, [new(LevyType.ElectricityTax, Money.Eur(0.0205m), Jan1)])
            .ShouldHaveSingleItem().Type.ShouldBe(LevyType.ElectricityTax);
    }

    [Fact]
    public void A_rate_change_on_1_January_splits_the_consumption_by_days()
    {
        // 1 Dec 2025 – 31 Jan 2026: 31 + 31 days, so 620 kWh are 310 kWh at each rate.
        Levy[] rates =
        [
            new(LevyType.NetworkFee, Money.Eur(0.09m), new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 31)),
            new(LevyType.NetworkFee, Money.Eur(0.10m), Jan1),
        ];

        var charges = LevyCalculator.Calculate(new BillingPeriod(new DateOnly(2025, 12, 1), new DateOnly(2026, 1, 31)), 620m, rates);

        charges.Select(c => (c.Period, c.QuantityKwh, c.RatePerKwh, c.Amount)).ShouldBe(
        [
            (BillingPeriod.Month(2025, 12), 310m, Money.Eur(0.09m), Money.Eur(27.90m)),
            (BillingPeriod.Month(2026, 1), 310m, Money.Eur(0.10m), Money.Eur(31.00m)),
        ]);
    }

    [Fact]
    public void Shared_consumption_still_adds_up_to_the_metered_total()
    {
        // Monthly rates over a quarter: 1000 kWh by 31/28/31 days.
        Levy[] rates =
        [
            new(LevyType.ConcessionFee, Money.Eur(0.0166m), Jan1, new DateOnly(2026, 1, 31)),
            new(LevyType.ConcessionFee, Money.Eur(0.0170m), new DateOnly(2026, 2, 1), new DateOnly(2026, 2, 28)),
            new(LevyType.ConcessionFee, Money.Eur(0.0180m), new DateOnly(2026, 3, 1)),
        ];

        var charges = LevyCalculator.Calculate(new BillingPeriod(Jan1, new DateOnly(2026, 3, 31)), 1000m, rates);

        charges.Select(c => c.QuantityKwh).ShouldBe([344.44444m, 311.11112m, 344.44444m]);
        charges.Sum(c => c.QuantityKwh).ShouldBe(1000m);

        // 5.71777770 → 5.72, 5.28888904 → 5.29, 6.19999992 → 6.20
        charges.Select(c => c.Amount).ShouldBe([Money.Eur(5.72m), Money.Eur(5.29m), Money.Eur(6.20m)]);
    }

    [Fact]
    public void Rates_outside_the_period_are_ignored()
    {
        Levy[] rates =
        [
            new(LevyType.ElectricityTax, Money.Eur(0.0300m), new DateOnly(2020, 1, 1), new DateOnly(2025, 12, 31)),
            new(LevyType.ElectricityTax, Money.Eur(0.0205m), Jan1),
        ];

        LevyCalculator.Calculate(BillingPeriod.Month(2026, 6), 300m, rates)
            .ShouldHaveSingleItem().Amount.ShouldBe(Money.Eur(6.15m));
    }

    [Fact]
    public void Zero_consumption_gives_zero_amounts()
    {
        LevyCalculator.Calculate(BillingPeriod.Month(2026, 6), 0m, Rates2026)
            .ShouldAllBe(c => c.Amount == Money.Eur(0m));
    }

    [Fact]
    public void A_day_without_a_rate_is_refused()
    {
        Levy[] rates =
        [
            new(LevyType.NetworkFee, Money.Eur(0.09m), new DateOnly(2025, 1, 1), new DateOnly(2025, 12, 30)),
            new(LevyType.NetworkFee, Money.Eur(0.10m), Jan1),
        ];

        var ex = Should.Throw<InvalidOperationException>(() =>
            LevyCalculator.Calculate(BillingPeriod.Month(2025, 12), 100m, rates.Take(1)));
        ex.Message.ShouldContain("2025-12-31");

        Should.Throw<InvalidOperationException>(() =>
            LevyCalculator.Calculate(BillingPeriod.Month(2026, 1), 100m, [new(LevyType.NetworkFee, Money.Eur(0.10m), new DateOnly(2026, 1, 2))]))
            .Message.ShouldContain("2026-01-01");
    }

    [Fact]
    public void Two_rates_on_one_day_are_refused()
    {
        // An open-ended old rate and a new one: someone forgot to close the old rate.
        Levy[] rates =
        [
            new(LevyType.NetworkFee, Money.Eur(0.09m), new DateOnly(2025, 1, 1)),
            new(LevyType.NetworkFee, Money.Eur(0.10m), Jan1),
        ];

        Should.Throw<InvalidOperationException>(() => LevyCalculator.Calculate(BillingPeriod.Month(2026, 1), 100m, rates))
            .Message.ShouldContain("2026-01-01");

        Levy[] overlapping =
        [
            new(LevyType.NetworkFee, Money.Eur(0.09m), new DateOnly(2025, 1, 1), new DateOnly(2026, 1, 10)),
            new(LevyType.NetworkFee, Money.Eur(0.10m), Jan1),
        ];

        Should.Throw<InvalidOperationException>(() => LevyCalculator.Calculate(BillingPeriod.Month(2026, 1), 100m, overlapping));
    }

    [Fact]
    public void Negative_or_too_precise_consumption_is_refused()
    {
        Should.Throw<ArgumentException>(() => LevyCalculator.Calculate(BillingPeriod.Year(2026), -1m, Rates2026));
        Should.Throw<ArgumentException>(() => LevyCalculator.Calculate(BillingPeriod.Year(2026), 1.123456m, Rates2026));
    }
}
