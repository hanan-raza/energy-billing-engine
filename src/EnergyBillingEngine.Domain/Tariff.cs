namespace EnergyBillingEngine.Domain;

/// <summary>Electricity tariff (Stromtarif): a yearly base price plus an energy price per kWh.</summary>
/// <remarks>
/// Optional time-of-use bands (e.g. a cheaper night rate, Hoch-/Niedertarif) override the energy price
/// for part of the day. Prices are net, without VAT.
/// </remarks>
public sealed class Tariff
{
    /// <summary>Maximum length of <see cref="Name"/>.</summary>
    public const int NameMaxLength = 100;

    private readonly List<TimeOfUseBand> _timeOfUseBands = [];

    /// <summary>Creates a tariff without time-of-use bands.</summary>
    /// <param name="name">Product name shown to the customer.</param>
    /// <param name="basePricePerYear">Base price (Grundpreis) per year, independent of consumption.</param>
    /// <param name="energyPricePerKwh">Energy price (Arbeitspreis) per kWh.</param>
    /// <exception cref="ArgumentException">A price is negative, or the two prices use different currencies.</exception>
    public Tariff(string name, Money basePricePerYear, Money energyPricePerKwh)
    {
        ArgumentNullException.ThrowIfNull(basePricePerYear);
        ArgumentNullException.ThrowIfNull(energyPricePerKwh);
        EnsureNotNegative(basePricePerYear, nameof(basePricePerYear));
        EnsureNotNegative(energyPricePerKwh, nameof(energyPricePerKwh));
        if (basePricePerYear.Currency != energyPricePerKwh.Currency)
        {
            throw new ArgumentException("Base price and energy price must use the same currency.", nameof(energyPricePerKwh));
        }

        Name = Text.Required(name, NameMaxLength, nameof(name));
        BasePricePerYear = basePricePerYear;
        EnergyPricePerKwh = energyPricePerKwh;
    }

    private Tariff()
    {
    }

    /// <summary>Technical key, assigned when the tariff is added to the database context.</summary>
    public Guid Id { get; private set; }

    /// <summary>Product name.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Base price (Grundpreis) per year.</summary>
    public Money BasePricePerYear { get; private set; } = null!;

    /// <summary>Energy price (Arbeitspreis) per kWh outside any time-of-use band.</summary>
    public Money EnergyPricePerKwh { get; private set; } = null!;

    /// <summary>Currency of all prices of this tariff.</summary>
    public string Currency => EnergyPricePerKwh.Currency;

    /// <summary>Parts of the day with their own energy price.</summary>
    public IReadOnlyList<TimeOfUseBand> TimeOfUseBands => _timeOfUseBands;

    /// <summary>Adds a part of the day with its own energy price, in German local (wall-clock) time.</summary>
    /// <param name="name">Short name, e.g. <c>NT</c> for the night rate (Niedertarif).</param>
    /// <param name="from">First minute of the band.</param>
    /// <param name="to">End of the band (excluded). May be earlier than <paramref name="from"/> for a band across midnight, e.g. 22:00–06:00.</param>
    /// <param name="energyPricePerKwh">Energy price within the band.</param>
    /// <exception cref="ArgumentException">The band is empty, overlaps another band, the price is negative, or the currency differs from the tariff's.</exception>
    public TimeOfUseBand AddTimeOfUseBand(string name, TimeOnly from, TimeOnly to, Money energyPricePerKwh)
    {
        ArgumentNullException.ThrowIfNull(energyPricePerKwh);
        EnsureNotNegative(energyPricePerKwh, nameof(energyPricePerKwh));
        if (energyPricePerKwh.Currency != Currency)
        {
            throw new ArgumentException($"The band price must be in {Currency}.", nameof(energyPricePerKwh));
        }

        var band = new TimeOfUseBand(name, from, to, energyPricePerKwh);
        var clash = _timeOfUseBands.FirstOrDefault(band.Overlaps);
        if (clash is not null)
        {
            // Overlapping bands would make the price of a quarter hour depend on the order of the bands.
            throw new ArgumentException($"Band {band.Name} overlaps band {clash.Name}.", nameof(from));
        }

        _timeOfUseBands.Add(band);
        return band;
    }

    /// <summary>Energy price per kWh at <paramref name="localTime"/> (German wall-clock time).</summary>
    public Money EnergyPriceAt(TimeOnly localTime) =>
        _timeOfUseBands.FirstOrDefault(b => b.Contains(localTime))?.EnergyPricePerKwh ?? EnergyPricePerKwh;

    private static void EnsureNotNegative(Money price, string parameterName)
    {
        if (price.Amount < 0m)
        {
            throw new ArgumentException($"A price must not be negative ({price}).", parameterName);
        }
    }
}
