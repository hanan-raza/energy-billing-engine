namespace EnergyBillingEngine.Domain;

/// <summary>Net total, VAT and gross total of an invoice.</summary>
/// <param name="Net">Sum of the net line amounts.</param>
/// <param name="Rate">The applied rate.</param>
/// <param name="Vat">VAT on <paramref name="Net"/>, rounded once.</param>
public sealed record VatCalculation(Money Net, VatRate Rate, Money Vat)
{
    /// <summary>Amount the customer pays (Bruttobetrag).</summary>
    public Money Gross => Net + Vat;
}

/// <summary>Applies VAT (Umsatzsteuer) to the net amounts of an invoice.</summary>
public static class VatEngine
{
    /// <summary>
    /// Adds up the net amounts and computes the VAT on that total, rounded commercially to cents.
    /// </summary>
    /// <remarks>
    /// VAT is computed once on the net total per rate, as printed on the invoice, and not per line: three
    /// lines of 0.08 EUR give 0.05 EUR VAT on 0.24 EUR, while VAT rounded per line would add up to 0.06 EUR.
    /// Credit notes (negative totals) round away from zero symmetrically.
    /// </remarks>
    /// <param name="netAmounts">Net line amounts, already rounded to cents.</param>
    /// <param name="rate">Rate of the contract.</param>
    /// <param name="currency">Currency of the invoice; an empty invoice totals zero in it.</param>
    /// <exception cref="ArgumentException">An amount has more than two decimal places.</exception>
    /// <exception cref="InvalidOperationException">An amount is in another currency.</exception>
    public static VatCalculation Calculate(IEnumerable<Money> netAmounts, VatRate rate, string currency)
    {
        ArgumentNullException.ThrowIfNull(netAmounts);
        ArgumentNullException.ThrowIfNull(rate);

        var amounts = netAmounts.ToList();
        if (amounts.Any(a => decimal.Round(a.Amount, Precision.AmountDecimals) != a.Amount))
        {
            // VAT belongs on the printed net total; unrounded amounts would tax a total nobody sees.
            throw new ArgumentException("Net amounts must be rounded to cents before VAT is applied.", nameof(netAmounts));
        }

        var net = Money.Sum(amounts, currency);
        return new VatCalculation(net, rate, rate.TaxOn(net));
    }
}
