namespace EnergyBillingEngine.Domain;

/// <summary>Billing period (Abrechnungszeitraum), in German calendar days, both ends included.</summary>
/// <remarks>
/// Periods are calendar days, not instants: a contract runs "from 1 March to 31 December", and
/// pro-rata prices are computed per day. The 23- and 25-hour days around a clock change still count
/// as one day each; only interval billing (Day 35) needs the exact hours.
/// </remarks>
public sealed record BillingPeriod
{
    /// <summary>Creates a period.</summary>
    /// <param name="start">First day of the period.</param>
    /// <param name="end">Last day of the period (included).</param>
    /// <exception cref="ArgumentException"><paramref name="end"/> is before <paramref name="start"/>.</exception>
    public BillingPeriod(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            throw new ArgumentException($"The period ends ({end:yyyy-MM-dd}) before it starts ({start:yyyy-MM-dd}).", nameof(end));
        }

        Start = start;
        End = end;
    }

    /// <summary>First day of the period.</summary>
    public DateOnly Start { get; }

    /// <summary>Last day of the period (included).</summary>
    public DateOnly End { get; }

    /// <summary>Number of days, both ends included; a 1-day period has <c>Days == 1</c>.</summary>
    public int Days => End.DayNumber - Start.DayNumber + 1;

    /// <summary>True if <paramref name="day"/> lies within the period.</summary>
    public bool Contains(DateOnly day) => day >= Start && day <= End;

    /// <summary>One calendar month, e.g. <c>Month(2026, 2)</c> is 1–28 February 2026.</summary>
    public static BillingPeriod Month(int year, int month)
    {
        var first = new DateOnly(year, month, 1);
        return new BillingPeriod(first, first.AddMonths(1).AddDays(-1));
    }

    /// <summary>One calendar year, 1 January to 31 December.</summary>
    public static BillingPeriod Year(int year) => new(new DateOnly(year, 1, 1), new DateOnly(year, 12, 31));
}
