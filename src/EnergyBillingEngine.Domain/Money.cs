using System.Globalization;

namespace EnergyBillingEngine.Domain;

/// <summary>An amount of money in one currency (Geldbetrag).</summary>
/// <remarks>
/// A value is not rounded when it is created: energy prices carry more than two decimals
/// (e.g. 0.324517 EUR/kWh), and rounding every intermediate result would let cent errors add up
/// over thousands of kWh. Amounts are rounded once, explicitly, where an invoice amount is fixed.
/// </remarks>
public sealed record Money
{
    /// <summary>ISO 4217 code of the euro.</summary>
    public const string Euro = "EUR";

    /// <summary>Creates an amount.</summary>
    /// <param name="amount">The amount, with as many decimals as it needs.</param>
    /// <param name="currency">Three upper-case letters, e.g. <c>EUR</c> or <c>CHF</c>.</param>
    /// <exception cref="ArgumentException">The currency is not an ISO 4217-style code.</exception>
    public Money(decimal amount, string currency)
    {
        if (!IsCurrencyCode(currency))
        {
            throw new ArgumentException($"'{currency}' is not a three-letter ISO 4217 currency code.", nameof(currency));
        }

        Amount = amount;
        Currency = currency;
    }

    /// <summary>The amount.</summary>
    public decimal Amount { get; }

    /// <summary>ISO 4217 currency code.</summary>
    public string Currency { get; }

    /// <summary>An amount in euro.</summary>
    public static Money Eur(decimal amount) => new(amount, Euro);

    /// <summary>Zero in <paramref name="currency"/>; the neutral start value for sums.</summary>
    public static Money Zero(string currency) => new(0m, currency);

    /// <summary>True for three upper-case ASCII letters. The list of codes in use is not checked.</summary>
    public static bool IsCurrencyCode(string? value) =>
        value is { Length: 3 } && value.All(char.IsAsciiLetterUpper);

    /// <summary>Sum of <paramref name="amounts"/>; zero in <paramref name="currency"/> if there are none.</summary>
    /// <exception cref="InvalidOperationException">An amount is in another currency.</exception>
    public static Money Sum(IEnumerable<Money> amounts, string currency)
    {
        ArgumentNullException.ThrowIfNull(amounts);
        return amounts.Aggregate(Zero(currency), (total, next) => total.Add(next));
    }

    /// <summary>Adds an amount in the same currency.</summary>
    /// <exception cref="InvalidOperationException">The currencies differ.</exception>
    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    /// <summary>Subtracts an amount in the same currency.</summary>
    /// <exception cref="InvalidOperationException">The currencies differ.</exception>
    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    /// <summary>Multiplies by a quantity, e.g. a price per kWh by the consumption. Not rounded.</summary>
    public Money Multiply(decimal factor) => new(Amount * factor, Currency);

    /// <summary>The same amount with the opposite sign, e.g. for a credit note line.</summary>
    public Money Negate() => new(-Amount, Currency);

    /// <summary>Rounds to cents with commercial rounding (kaufmännisches Runden).</summary>
    /// <remarks>
    /// German invoices round half away from zero (0.125 → 0.13). .NET's default, banker's rounding
    /// (0.125 → 0.12), is statistically fairer over many values but differs from what a customer or
    /// clerk gets when recomputing a line by hand, which is what an invoice has to survive.
    /// </remarks>
    public Money RoundToCents() => Round(2, MidpointRounding.AwayFromZero);

    /// <summary>Rounds to <paramref name="decimals"/> places with an explicit midpoint rule.</summary>
    public Money Round(int decimals, MidpointRounding mode) => new(Math.Round(Amount, decimals, mode), Currency);

    /// <summary>Same as <see cref="Add"/>.</summary>
    public static Money operator +(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.Add(right);
    }

    /// <summary>Same as <see cref="Subtract"/>.</summary>
    public static Money operator -(Money left, Money right)
    {
        ArgumentNullException.ThrowIfNull(left);
        return left.Subtract(right);
    }

    /// <summary>Same as <see cref="Negate"/>.</summary>
    public static Money operator -(Money value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Negate();
    }

    /// <summary>Same as <see cref="Multiply"/>.</summary>
    public static Money operator *(Money value, decimal factor)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Multiply(factor);
    }

    /// <summary>Same as <see cref="Multiply"/>.</summary>
    public static Money operator *(decimal factor, Money value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Multiply(factor);
    }

    /// <summary>Amount and currency, e.g. <c>12.34 EUR</c>, independent of the current culture.</summary>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Amount} {Currency}");

    private void EnsureSameCurrency(Money other)
    {
        ArgumentNullException.ThrowIfNull(other);
        if (other.Currency != Currency)
        {
            // Silently adding EUR to CHF would produce a number that is wrong in both currencies.
            throw new InvalidOperationException($"Cannot combine {Currency} and {other.Currency} amounts.");
        }
    }
}
