namespace EnergyBillingEngine.Domain;

/// <summary>Part of a billing period during which one tariff applies.</summary>
/// <param name="Period">The calendar days of this part, both ends included.</param>
/// <param name="Tariff">The tariff in effect on every one of these days.</param>
public sealed record TariffPeriod(BillingPeriod Period, Tariff Tariff)
{
    /// <summary>Calendar days, the basis of pro-rata base prices (a 23- or 25-hour day is one day).</summary>
    public int Days => Period.Days;

    /// <summary>Local midnight at the start of the first day, Europe/Berlin.</summary>
    public DateTimeOffset Start => GermanCalendar.StartOfDay(Period.Start);

    /// <summary>Local midnight after the last day (exclusive), Europe/Berlin.</summary>
    public DateTimeOffset End => GermanCalendar.StartOfDay(Period.End.AddDays(1));

    /// <summary>Real elapsed time; differs from <see cref="Days"/> × 24 h when the part contains a clock change.</summary>
    public TimeSpan Duration => End - Start;
}
