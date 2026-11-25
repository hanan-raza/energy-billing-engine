namespace EnergyBillingEngine.Domain;

/// <summary>A part of the day with its own energy price (Zeitzone, e.g. Hochtarif/Niedertarif).</summary>
/// <remarks>
/// Times are German wall-clock times, because that is how tariffs are sold ("night rate from 22:00").
/// On the 25-hour October day the repeated hour 02:00–03:00 therefore gets the night price twice.
/// </remarks>
public sealed class TimeOfUseBand
{
    /// <summary>Maximum length of <see cref="Name"/>.</summary>
    public const int NameMaxLength = 20;

    internal TimeOfUseBand(string name, TimeOnly from, TimeOnly to, Money energyPricePerKwh)
    {
        if (from == to)
        {
            throw new ArgumentException("A time-of-use band must not be empty; leave it out to use the normal energy price.", nameof(to));
        }

        Name = Text.Required(name, NameMaxLength, nameof(name));
        From = from;
        To = to;
        EnergyPricePerKwh = energyPricePerKwh;
    }

    private TimeOfUseBand()
    {
    }

    /// <summary>Technical key.</summary>
    public Guid Id { get; private set; }

    /// <summary>Short name, e.g. <c>NT</c>.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>First minute of the band.</summary>
    public TimeOnly From { get; private set; }

    /// <summary>End of the band (excluded); earlier than <see cref="From"/> for a band across midnight.</summary>
    public TimeOnly To { get; private set; }

    /// <summary>Energy price (Arbeitspreis) per kWh within the band.</summary>
    public Money EnergyPricePerKwh { get; private set; } = null!;

    /// <summary>True if <paramref name="localTime"/> falls into the band.</summary>
    public bool Contains(TimeOnly localTime) =>
        From < To
            ? localTime >= From && localTime < To
            : localTime >= From || localTime < To;

    /// <summary>True if the two bands share at least one minute.</summary>
    /// <remarks>Two arcs of a circle overlap exactly when one starts inside the other.</remarks>
    public bool Overlaps(TimeOfUseBand other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return Contains(other.From) || other.Contains(From);
    }
}
