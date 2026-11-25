namespace EnergyBillingEngine.Domain;

/// <summary>Decimal places stored for prices, quantities and amounts; the database columns use the same values.</summary>
/// <remarks>
/// SQL Server and Oracle round a decimal to the column's scale without an error. A unit price of
/// 120/365 EUR would be stored as 0.328767 while the line amount was computed from the full value,
/// so the reloaded line would no longer multiply out to its own amount. Values with more places are
/// therefore refused in the domain: rounding is a business decision and has to be made explicitly.
/// </remarks>
public static class Precision
{
    /// <summary>Total digits of every decimal column.</summary>
    public const int Digits = 18;

    /// <summary>Places of a price or rate per unit, e.g. 0.324517 EUR/kWh.</summary>
    public const int PriceDecimals = 6;

    /// <summary>Places of a quantity, e.g. kWh; matches the 15-minute values of meter-data-service.</summary>
    public const int QuantityDecimals = 5;

    /// <summary>Places of an amount the customer pays (cents).</summary>
    public const int AmountDecimals = 2;

    internal static void EnsurePrice(Money price, string parameterName)
    {
        if (!HasAtMost(price.Amount, PriceDecimals))
        {
            throw new ArgumentException($"A price may have at most {PriceDecimals} decimal places ({price}); round it explicitly first.", parameterName);
        }
    }

    internal static void EnsureQuantity(decimal quantity, string parameterName)
    {
        if (!HasAtMost(quantity, QuantityDecimals))
        {
            throw new ArgumentException($"A quantity may have at most {QuantityDecimals} decimal places ({quantity}); round it explicitly first.", parameterName);
        }
    }

    // Trailing zeros don't count: 0.300000000m is a 1-place value.
    private static bool HasAtMost(decimal value, int decimals) => decimal.Round(value, decimals) == value;
}
