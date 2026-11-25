namespace EnergyBillingEngine.Domain;

/// <summary>A per-kWh levy, tax or regulated charge (Umlage, Abgabe, Entgelt) with the days it applies to.</summary>
/// <remarks>
/// Rates change by law, usually on 1 January, so each rate carries its own validity. A billing period
/// across such a change is split per rate (Day 33).
/// </remarks>
public sealed class Levy
{
    /// <summary>Creates a rate.</summary>
    /// <param name="type">Which charge this is.</param>
    /// <param name="ratePerKwh">Net amount per kWh.</param>
    /// <param name="validFrom">First day the rate applies.</param>
    /// <param name="validTo">Last day the rate applies, or <c>null</c> until further notice.</param>
    /// <exception cref="ArgumentException"><paramref name="validTo"/> is before <paramref name="validFrom"/>.</exception>
    public Levy(LevyType type, Money ratePerKwh, DateOnly validFrom, DateOnly? validTo = null)
    {
        ArgumentNullException.ThrowIfNull(ratePerKwh);
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown levy type.");
        }

        if (validTo < validFrom)
        {
            throw new ArgumentException($"The rate ends ({validTo:yyyy-MM-dd}) before it starts ({validFrom:yyyy-MM-dd}).", nameof(validTo));
        }

        Type = type;
        RatePerKwh = ratePerKwh;
        ValidFrom = validFrom;
        ValidTo = validTo;
    }

    private Levy()
    {
    }

    /// <summary>Technical key.</summary>
    public Guid Id { get; private set; }

    /// <summary>Which charge this is.</summary>
    public LevyType Type { get; private set; }

    /// <summary>Net amount per kWh.</summary>
    public Money RatePerKwh { get; private set; } = null!;

    /// <summary>First day the rate applies.</summary>
    public DateOnly ValidFrom { get; private set; }

    /// <summary>Last day the rate applies, or <c>null</c> until further notice.</summary>
    public DateOnly? ValidTo { get; private set; }

    /// <summary>True if the rate applies on <paramref name="day"/>.</summary>
    public bool IsValidOn(DateOnly day) => day >= ValidFrom && (ValidTo is null || day <= ValidTo);
}
