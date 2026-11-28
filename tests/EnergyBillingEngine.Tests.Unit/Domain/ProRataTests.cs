using EnergyBillingEngine.Domain;
using Shouldly;

namespace EnergyBillingEngine.Tests.Unit.Domain;

public sealed class ProRataTests
{
    [Fact]
    public void Thirds_add_up_to_the_total_exactly()
    {
        // Rounding each third on its own gives 333.33333 × 3 = 999.99999.
        ProRata.Distribute(1000m, [1, 1, 1], 5).ShouldBe([333.33333m, 333.33334m, 333.33333m]);
    }

    [Fact]
    public void A_quarter_shared_by_month_days_follows_the_days()
    {
        // January–March 2026: 31 + 28 + 31 = 90 days.
        var shares = ProRata.Distribute(1000m, [31, 28, 31], 5);

        shares.ShouldBe([344.44444m, 311.11112m, 344.44444m]);
        shares.Sum().ShouldBe(1000m);
    }

    [Fact]
    public void Even_splits_stay_exact()
    {
        ProRata.Distribute(3650m, [31, 334], 5).ShouldBe([310m, 3340m]);
    }

    [Fact]
    public void A_zero_weight_gets_nothing()
    {
        ProRata.Distribute(10m, [0, 1, 0], 5).ShouldBe([0m, 10m, 0m]);
    }

    [Fact]
    public void Shares_always_add_up_and_are_never_negative()
    {
        var random = new Random(34);
        for (var run = 0; run < 1000; run++)
        {
            var total = decimal.Round((decimal)random.NextDouble() * 100_000m, 5);
            var weights = Enumerable.Range(0, random.Next(1, 13)).Select(_ => random.Next(0, 32)).Append(1).ToArray();

            var shares = ProRata.Distribute(total, weights, 5);

            shares.Sum().ShouldBe(total);
            shares.ShouldAllBe(s => s >= 0m && decimal.Round(s, 5) == s);
        }
    }

    [Fact]
    public void Invalid_input_is_refused()
    {
        Should.Throw<ArgumentException>(() => ProRata.Distribute(1m, [], 5));
        Should.Throw<ArgumentException>(() => ProRata.Distribute(1m, [1, -1], 5));
        Should.Throw<ArgumentException>(() => ProRata.Distribute(1m, [0, 0], 5));
        Should.Throw<ArgumentException>(() => ProRata.Distribute(1.123456m, [1], 5));
    }
}
