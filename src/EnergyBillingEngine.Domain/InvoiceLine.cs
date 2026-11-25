namespace EnergyBillingEngine.Domain;

/// <summary>One line of an invoice (Rechnungsposition): quantity × unit price, rounded to cents.</summary>
public sealed class InvoiceLine
{
    /// <summary>Maximum length of <see cref="Description"/>.</summary>
    public const int DescriptionMaxLength = 200;

    /// <summary>Maximum length of <see cref="Unit"/>.</summary>
    public const int UnitMaxLength = 10;

    internal InvoiceLine(int position, InvoiceLineType type, string description, decimal quantity, string unit, Money unitPrice)
    {
        Position = position;
        ArgumentNullException.ThrowIfNull(unitPrice);
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown invoice line type.");
        }

        Precision.EnsurePrice(unitPrice, nameof(unitPrice));
        Precision.EnsureQuantity(quantity, nameof(quantity));

        Type = type;
        Description = Text.Required(description, DescriptionMaxLength, nameof(description));
        Quantity = quantity;
        Unit = Text.Required(unit, UnitMaxLength, nameof(unit));
        UnitPrice = unitPrice;

        // The line amount is what the customer sees and what the totals add up; it is rounded here,
        // once, so the sum of the printed lines always equals the printed total.
        Amount = unitPrice.Multiply(quantity).RoundToCents();
    }

    private InvoiceLine()
    {
    }

    /// <summary>Technical key.</summary>
    public Guid Id { get; private set; }

    /// <summary>Line number on the invoice (Positionsnummer), starting at 1.</summary>
    public int Position { get; private set; }

    /// <summary>What the line charges for.</summary>
    public InvoiceLineType Type { get; private set; }

    /// <summary>Text printed on the invoice.</summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>Billed quantity, e.g. kWh or days.</summary>
    public decimal Quantity { get; private set; }

    /// <summary>Unit of <see cref="Quantity"/>, e.g. <c>kWh</c> or <c>days</c>.</summary>
    public string Unit { get; private set; } = string.Empty;

    /// <summary>Net price per unit, not rounded.</summary>
    public Money UnitPrice { get; private set; } = null!;

    /// <summary>Net line amount, rounded to cents.</summary>
    public Money Amount { get; private set; } = null!;
}
