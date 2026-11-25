namespace EnergyBillingEngine.Domain;

/// <summary>Supply contract (Liefervertrag) for one market location under one tariff.</summary>
public sealed class Contract
{
    /// <summary>Maximum length of <see cref="ContractNumber"/>.</summary>
    public const int ContractNumberMaxLength = 20;

    internal Contract(string contractNumber, string marketLocationId, Tariff tariff, DateOnly startDate)
    {
        ArgumentNullException.ThrowIfNull(tariff);
        if (!Domain.MarketLocationId.IsValid(marketLocationId))
        {
            throw new ArgumentException($"'{marketLocationId}' is not a valid market location ID (MaLo-ID).", nameof(marketLocationId));
        }

        ContractNumber = Text.Required(contractNumber, ContractNumberMaxLength, nameof(contractNumber));
        MarketLocationId = marketLocationId;
        Tariff = tariff;
        StartDate = startDate;
    }

    private Contract()
    {
    }

    /// <summary>Technical key, assigned when the contract is added to the database context.</summary>
    public Guid Id { get; private set; }

    /// <summary>The customer who signed the contract.</summary>
    public Guid CustomerId { get; private set; }

    /// <summary>Contract number (Vertragsnummer).</summary>
    public string ContractNumber { get; private set; } = string.Empty;

    /// <summary>The supplied market location (Marktlokation, MaLo-ID).</summary>
    public string MarketLocationId { get; private set; } = string.Empty;

    /// <summary>Key of <see cref="Tariff"/>.</summary>
    public Guid TariffId { get; private set; }

    /// <summary>The agreed tariff. Must be loaded explicitly when the contract comes from the database.</summary>
    public Tariff Tariff { get; private set; } = null!;

    /// <summary>First day of supply (Lieferbeginn).</summary>
    public DateOnly StartDate { get; private set; }

    /// <summary>Last day of supply (Lieferende), or <c>null</c> while the contract runs open-ended.</summary>
    public DateOnly? EndDate { get; private set; }

    /// <summary>True if the market location is supplied under this contract on <paramref name="day"/>.</summary>
    public bool IsActiveOn(DateOnly day) => day >= StartDate && (EndDate is null || day <= EndDate);

    /// <summary>True if the contract supplies on at least one day of <paramref name="period"/>.</summary>
    public bool Overlaps(BillingPeriod period)
    {
        ArgumentNullException.ThrowIfNull(period);
        return StartDate <= period.End && (EndDate is null || EndDate >= period.Start);
    }

    /// <summary>Ends supply after <paramref name="lastDay"/> (Kündigung, Auszug, Lieferantenwechsel).</summary>
    /// <exception cref="ArgumentException"><paramref name="lastDay"/> is before the start of supply.</exception>
    /// <exception cref="InvalidOperationException">The contract has already ended.</exception>
    public void Terminate(DateOnly lastDay)
    {
        if (EndDate is not null)
        {
            throw new InvalidOperationException($"Contract {ContractNumber} already ended on {EndDate:yyyy-MM-dd}.");
        }

        if (lastDay < StartDate)
        {
            throw new ArgumentException($"Supply cannot end ({lastDay:yyyy-MM-dd}) before it starts ({StartDate:yyyy-MM-dd}).", nameof(lastDay));
        }

        EndDate = lastDay;
    }
}
