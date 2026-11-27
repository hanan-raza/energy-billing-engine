namespace EnergyBillingEngine.Domain;

/// <summary>
/// German legal time (gesetzliche Zeit, Europe/Berlin). A billing day runs from local midnight to the
/// next local midnight, so it lasts 23, 24 or 25 hours.
/// </summary>
public static class GermanCalendar
{
    /// <summary>IANA zone ID, resolved on Windows and Linux alike since .NET 6.</summary>
    public const string TimeZoneId = "Europe/Berlin";

    /// <summary>The German time zone, including its DST rules.</summary>
    public static TimeZoneInfo TimeZone { get; } = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);

    /// <summary>Local midnight at the start of <paramref name="day"/>, with the German offset of that moment (+01:00 or +02:00).</summary>
    public static DateTimeOffset StartOfDay(DateOnly day)
    {
        // Germany switches at 02:00 and 03:00, so local midnight always exists exactly once.
        var midnight = day.ToDateTime(TimeOnly.MinValue);
        return new DateTimeOffset(midnight, TimeZone.GetUtcOffset(midnight));
    }

    /// <summary>Length of the local day: 23 hours on the last Sunday of March, 25 on the last Sunday of October.</summary>
    public static TimeSpan LengthOf(DateOnly day) => StartOfDay(day.AddDays(1)) - StartOfDay(day);
}
