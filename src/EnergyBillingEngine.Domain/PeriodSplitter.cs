namespace EnergyBillingEngine.Domain;

/// <summary>
/// Splits a billing period at tariff changes, the first step of pro-rata billing (zeitanteilige Abrechnung).
/// </summary>
public static class PeriodSplitter
{
    /// <summary>
    /// Returns the parts of <paramref name="period"/> with the tariff in effect on each day, in date order.
    /// </summary>
    /// <param name="period">The billed days.</param>
    /// <param name="changes">
    /// The tariff history of the contract, in any order. It must contain the change in effect on the first day
    /// of the period (the latest one on or before it); earlier changes and changes after the period are ignored.
    /// </param>
    /// <returns>Parts without gaps or overlaps whose days add up to <see cref="BillingPeriod.Days"/>.</returns>
    /// <exception cref="ArgumentException">Two changes take effect on the same day.</exception>
    /// <exception cref="InvalidOperationException">No tariff is in effect on the first day of the period.</exception>
    public static IReadOnlyList<TariffPeriod> Split(BillingPeriod period, IEnumerable<TariffChange> changes)
    {
        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(changes);

        var ordered = changes.OrderBy(c => c.EffectiveFrom).ToList();
        for (var i = 1; i < ordered.Count; i++)
        {
            if (ordered[i].EffectiveFrom == ordered[i - 1].EffectiveFrom)
            {
                // Two tariffs for one day would make the result depend on input order.
                throw new ArgumentException(
                    $"Two tariff changes take effect on {ordered[i].EffectiveFrom:yyyy-MM-dd}.", nameof(changes));
            }
        }

        var startIndex = ordered.FindLastIndex(c => c.EffectiveFrom <= period.Start);
        if (startIndex < 0)
        {
            // Not filled with the first later tariff: days without a tariff are a contract that started
            // mid-period, and the caller must shorten the period instead of billing days nobody agreed to.
            throw new InvalidOperationException($"No tariff is in effect on {period.Start:yyyy-MM-dd}, the first day of the period.");
        }

        var inPeriod = ordered.Skip(startIndex).TakeWhile(c => c.EffectiveFrom <= period.End).ToList();
        var parts = new List<TariffPeriod>(inPeriod.Count);
        for (var i = 0; i < inPeriod.Count; i++)
        {
            var first = i == 0 ? period.Start : inPeriod[i].EffectiveFrom;
            var last = i + 1 < inPeriod.Count ? inPeriod[i + 1].EffectiveFrom.AddDays(-1) : period.End;
            parts.Add(new TariffPeriod(new BillingPeriod(first, last), inPeriod[i].Tariff));
        }

        return parts;
    }
}
