namespace EnergyBillingEngine.Domain;

/// <summary>One itemised levy charge: the consumption of some days at the rate valid on those days.</summary>
/// <param name="Type">Which levy, tax or network charge.</param>
/// <param name="Period">The days this rate was applied to.</param>
/// <param name="QuantityKwh">Consumption share of these days, at most 5 decimal places.</param>
/// <param name="RatePerKwh">Net rate per kWh.</param>
public sealed record LevyCharge(LevyType Type, BillingPeriod Period, decimal QuantityKwh, Money RatePerKwh)
{
    /// <summary>Net amount, rounded to cents the same way an <see cref="InvoiceLine"/> rounds it.</summary>
    public Money Amount => RatePerKwh.Multiply(QuantityKwh).RoundToCents();
}
