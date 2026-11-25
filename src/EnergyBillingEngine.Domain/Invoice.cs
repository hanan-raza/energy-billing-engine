namespace EnergyBillingEngine.Domain;

/// <summary>Invoice (Rechnung) for one contract and billing period.</summary>
/// <remarks>Lines are net amounts; VAT is added as its own line once the VAT engine exists (Day 34).</remarks>
public sealed class Invoice
{
    /// <summary>Maximum length of <see cref="InvoiceNumber"/>.</summary>
    public const int InvoiceNumberMaxLength = 20;

    private readonly List<InvoiceLine> _lines = [];

    /// <summary>Creates an invoice without lines.</summary>
    /// <param name="invoiceNumber">Business key printed on the invoice (Rechnungsnummer).</param>
    /// <param name="contract">The billed contract.</param>
    /// <param name="period">The billed days; must overlap the contract's supply.</param>
    /// <param name="currency">Currency of every line.</param>
    /// <exception cref="ArgumentException">The period lies outside the contract, or a value is invalid.</exception>
    public Invoice(string invoiceNumber, Contract contract, BillingPeriod period, string currency = Money.Euro)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(period);
        if (!contract.Overlaps(period))
        {
            throw new ArgumentException(
                $"Contract {contract.ContractNumber} does not supply between {period.Start:yyyy-MM-dd} and {period.End:yyyy-MM-dd}.",
                nameof(period));
        }

        if (!Money.IsCurrencyCode(currency))
        {
            throw new ArgumentException($"'{currency}' is not a three-letter ISO 4217 currency code.", nameof(currency));
        }

        InvoiceNumber = Text.Required(invoiceNumber, InvoiceNumberMaxLength, nameof(invoiceNumber));
        Contract = contract;
        Period = period;
        Currency = currency;
    }

    private Invoice()
    {
    }

    /// <summary>Technical key.</summary>
    public Guid Id { get; private set; }

    /// <summary>Invoice number (Rechnungsnummer).</summary>
    public string InvoiceNumber { get; private set; } = string.Empty;

    /// <summary>Key of <see cref="Contract"/>.</summary>
    public Guid ContractId { get; private set; }

    /// <summary>The billed contract. Must be loaded explicitly when the invoice comes from the database.</summary>
    public Contract Contract { get; private set; } = null!;

    /// <summary>The billed days.</summary>
    public BillingPeriod Period { get; private set; } = null!;

    /// <summary>Currency of every line.</summary>
    public string Currency { get; private set; } = Money.Euro;

    /// <summary>Lines in the order they were added.</summary>
    /// <remarks>Sorted by <see cref="InvoiceLine.Position"/>, because a database returns rows in no guaranteed order.</remarks>
    public IReadOnlyList<InvoiceLine> Lines => [.. _lines.OrderBy(l => l.Position)];

    /// <summary>Sum of the (already rounded) line amounts.</summary>
    public Money NetTotal => Money.Sum(_lines.Select(l => l.Amount), Currency);

    /// <summary>Adds a line; its amount is <paramref name="quantity"/> × <paramref name="unitPrice"/>, rounded to cents.</summary>
    /// <exception cref="ArgumentException">The price is in another currency, or a value is invalid.</exception>
    public InvoiceLine AddLine(InvoiceLineType type, string description, decimal quantity, string unit, Money unitPrice)
    {
        ArgumentNullException.ThrowIfNull(unitPrice);
        if (unitPrice.Currency != Currency)
        {
            throw new ArgumentException($"The invoice is in {Currency}, the price in {unitPrice.Currency}.", nameof(unitPrice));
        }

        var line = new InvoiceLine(_lines.Count + 1, type, description, quantity, unit, unitPrice);
        _lines.Add(line);
        return line;
    }
}
