namespace EnergyBillingEngine.Domain;

/// <summary>Shares a quantity out by weights, e.g. a year's consumption by the days of each rate (zeitanteilige Aufteilung).</summary>
public static class ProRata
{
    /// <summary>
    /// Splits <paramref name="total"/> in proportion to <paramref name="weights"/>, each share rounded to
    /// <paramref name="decimals"/> places, so that the shares add up to exactly <paramref name="total"/>.
    /// </summary>
    /// <remarks>
    /// Each share is the difference of two rounded running totals (cumulative rounding). The rounding error
    /// never builds up, no share is negative, and equal weights next to each other differ by at most one
    /// unit in the last place. Rounding each share on its own would make 1000 kWh over 3 × 1/3 add up to 999.99999.
    /// </remarks>
    /// <exception cref="ArgumentException">There are no weights, a weight is negative, all weights are zero, or <paramref name="total"/> has more than <paramref name="decimals"/> places.</exception>
    public static IReadOnlyList<decimal> Distribute(decimal total, IReadOnlyList<int> weights, int decimals)
    {
        ArgumentNullException.ThrowIfNull(weights);
        if (weights.Count == 0)
        {
            throw new ArgumentException("At least one weight is needed.", nameof(weights));
        }

        if (weights.Any(w => w < 0))
        {
            throw new ArgumentException("Weights may not be negative.", nameof(weights));
        }

        long sum = weights.Sum(w => (long)w);
        if (sum == 0)
        {
            throw new ArgumentException("At least one weight must be greater than zero.", nameof(weights));
        }

        if (decimal.Round(total, decimals) != total)
        {
            throw new ArgumentException($"The total {total} has more than {decimals} decimal places.", nameof(total));
        }

        var shares = new decimal[weights.Count];
        long running = 0;
        var previous = 0m;
        for (var i = 0; i < weights.Count; i++)
        {
            running += weights[i];
            var cumulative = decimal.Round(total * running / sum, decimals, MidpointRounding.AwayFromZero);
            shares[i] = cumulative - previous;
            previous = cumulative;
        }

        return shares;
    }
}
