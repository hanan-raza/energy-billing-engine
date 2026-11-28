using System.Globalization;

namespace EnergyBillingEngine.Domain;

/// <summary>VAT rate (Umsatzsteuersatz) in percent, e.g. 19 % standard or 7 % reduced.</summary>
/// <remarks>Rates here are configuration, not law: the engine applies whatever rate the contract carries.</remarks>
public sealed record VatRate
{
    /// <summary>Decimal places of <see cref="Percent"/>; the database column uses the same.</summary>
    public const int PercentDecimals = 2;

    /// <summary>Creates a rate.</summary>
    /// <param name="percent">0 to 100, at most two decimal places (e.g. 19 or 7.5).</param>
    /// <exception cref="ArgumentOutOfRangeException">The rate is outside 0–100 % or has more than two decimal places.</exception>
    public VatRate(decimal percent)
    {
        if (percent is < 0m or > 100m || decimal.Round(percent, PercentDecimals) != percent)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), percent, "A VAT rate is 0–100 % with at most two decimal places.");
        }

        Percent = percent;
    }

    /// <summary>Standard rate (Regelsteuersatz), synthetic 19 %.</summary>
    public static VatRate Standard { get; } = new(19m);

    /// <summary>Reduced rate (ermäßigter Steuersatz), synthetic 7 %.</summary>
    public static VatRate Reduced { get; } = new(7m);

    /// <summary>The rate in percent.</summary>
    public decimal Percent { get; }

    /// <summary>VAT on <paramref name="net"/>, rounded commercially to cents.</summary>
    public Money TaxOn(Money net)
    {
        ArgumentNullException.ThrowIfNull(net);
        return net.Multiply(Percent / 100m).RoundToCents();
    }

    /// <summary>For example <c>19 %</c>, independent of the current culture.</summary>
    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Percent:0.##} %");
}
