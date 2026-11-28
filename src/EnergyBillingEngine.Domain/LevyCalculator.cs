namespace EnergyBillingEngine.Domain;

/// <summary>Applies per-kWh levies, taxes and network charges (Umlagen, Abgaben, Entgelte) to a consumption.</summary>
public static class LevyCalculator
{
    /// <summary>
    /// Returns one charge per levy type, or one per rate when a rate changes within the period,
    /// ordered by levy type and then by date.
    /// </summary>
    /// <param name="period">The billed days.</param>
    /// <param name="consumptionKwh">Consumption of the whole period, at most 5 decimal places.</param>
    /// <param name="rates">
    /// The configured rates. Every levy type that appears here must have exactly one rate on every day of the
    /// period; types that do not appear are not charged.
    /// </param>
    /// <remarks>
    /// When a rate changes inside the period, the consumption is shared out by days (linear
    /// Verbrauchsabgrenzung), because one meter reading per period cannot tell how much was used before the
    /// change. Interval billing could split by real values instead (Day 35). Rates of zero, like the EEG
    /// surcharge since July 2022, produce no charge, so the invoice does not print empty lines.
    /// </remarks>
    /// <exception cref="ArgumentException">The consumption is negative or has more than 5 decimal places.</exception>
    /// <exception cref="InvalidOperationException">A levy type has no rate, or two rates, on a day of the period.</exception>
    public static IReadOnlyList<LevyCharge> Calculate(BillingPeriod period, decimal consumptionKwh, IEnumerable<Levy> rates)
    {
        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(rates);
        if (consumptionKwh < 0)
        {
            throw new ArgumentException("Consumption may not be negative; feed-in is billed separately.", nameof(consumptionKwh));
        }

        Precision.EnsureQuantity(consumptionKwh, nameof(consumptionKwh));

        var charges = new List<LevyCharge>();
        foreach (var ofType in rates.GroupBy(r => r.Type).OrderBy(g => g.Key))
        {
            var segments = Segments(period, ofType.Key, ofType);
            var quantities = ProRata.Distribute(consumptionKwh, [.. segments.Select(s => s.Period.Days)], Precision.QuantityDecimals);
            for (var i = 0; i < segments.Count; i++)
            {
                if (segments[i].Rate.RatePerKwh.Amount != 0m)
                {
                    charges.Add(new LevyCharge(ofType.Key, segments[i].Period, quantities[i], segments[i].Rate.RatePerKwh));
                }
            }
        }

        return charges;
    }

    // The rates of one type that apply within the period, each clipped to the period; checked for gaps and overlaps.
    private static List<(BillingPeriod Period, Levy Rate)> Segments(BillingPeriod period, LevyType type, IEnumerable<Levy> rates)
    {
        var applicable = rates
            .Where(r => r.ValidFrom <= period.End && (r.ValidTo is null || r.ValidTo >= period.Start))
            .OrderBy(r => r.ValidFrom)
            .ToList();

        var segments = new List<(BillingPeriod, Levy)>(applicable.Count);
        var nextDay = period.Start;
        foreach (var rate in applicable)
        {
            if (rate.ValidFrom > nextDay)
            {
                throw new InvalidOperationException($"No {type} rate is configured for {nextDay:yyyy-MM-dd}.");
            }

            if (rate.ValidFrom < nextDay && segments.Count > 0)
            {
                throw new InvalidOperationException($"Two {type} rates apply on {rate.ValidFrom:yyyy-MM-dd}.");
            }

            var last = rate.ValidTo is { } validTo && validTo < period.End ? validTo : period.End;
            segments.Add((new BillingPeriod(nextDay, last), rate));
            if (last == period.End)
            {
                if (applicable.Count > segments.Count)
                {
                    // A later rate starts inside the period although this one is open-ended or runs past it.
                    throw new InvalidOperationException($"Two {type} rates apply on {applicable[segments.Count].ValidFrom:yyyy-MM-dd}.");
                }

                return segments;
            }

            nextDay = last.AddDays(1);
        }

        throw new InvalidOperationException($"No {type} rate is configured for {nextDay:yyyy-MM-dd}.");
    }
}
