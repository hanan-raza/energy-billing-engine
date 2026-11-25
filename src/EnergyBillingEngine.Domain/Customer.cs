namespace EnergyBillingEngine.Domain;

/// <summary>End customer (Endkunde, Letztverbraucher) who is supplied and invoiced.</summary>
public sealed class Customer
{
    /// <summary>Maximum length of <see cref="CustomerNumber"/>.</summary>
    public const int CustomerNumberMaxLength = 20;

    /// <summary>Maximum length of <see cref="Name"/>.</summary>
    public const int NameMaxLength = 200;

    private readonly List<Contract> _contracts = [];

    /// <summary>Creates a customer without contracts.</summary>
    /// <param name="customerNumber">Business key printed on invoices (Kundennummer).</param>
    /// <param name="name">Name of the person or company.</param>
    /// <exception cref="ArgumentException">A value is empty or too long.</exception>
    public Customer(string customerNumber, string name)
    {
        CustomerNumber = Text.Required(customerNumber, CustomerNumberMaxLength, nameof(customerNumber));
        Name = Text.Required(name, NameMaxLength, nameof(name));
    }

    private Customer()
    {
    }

    /// <summary>Technical key, assigned when the customer is added to the database context.</summary>
    public Guid Id { get; private set; }

    /// <summary>Customer number (Kundennummer).</summary>
    public string CustomerNumber { get; private set; } = string.Empty;

    /// <summary>Name of the person or company.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>All supply contracts, including ended ones.</summary>
    public IReadOnlyList<Contract> Contracts => _contracts;

    /// <summary>Signs a supply contract for one market location, starting on <paramref name="startDate"/>.</summary>
    /// <param name="contractNumber">Business key of the contract (Vertragsnummer).</param>
    /// <param name="marketLocationId">The supplied market location (MaLo-ID, 11 digits with check digit).</param>
    /// <param name="tariff">The agreed tariff.</param>
    /// <param name="startDate">First day of supply (Lieferbeginn).</param>
    /// <exception cref="ArgumentException">The contract number or MaLo-ID is invalid.</exception>
    /// <exception cref="InvalidOperationException">The market location is already supplied under another contract of this customer on or after <paramref name="startDate"/>.</exception>
    public Contract SignContract(string contractNumber, string marketLocationId, Tariff tariff, DateOnly startDate)
    {
        // A market location has exactly one supplier contract at a time; two overlapping contracts
        // would bill the same kWh twice.
        var overlapping = _contracts.FirstOrDefault(c =>
            c.MarketLocationId == marketLocationId && (c.EndDate is null || c.EndDate >= startDate));
        if (overlapping is not null)
        {
            throw new InvalidOperationException(
                $"Market location {marketLocationId} is already supplied under contract {overlapping.ContractNumber} on {startDate:yyyy-MM-dd}.");
        }

        var contract = new Contract(contractNumber, marketLocationId, tariff, startDate);
        _contracts.Add(contract);
        return contract;
    }
}
