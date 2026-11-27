namespace EnergyBillingEngine.Domain;

/// <summary>
/// A tariff that applies from a given day on (Preisanpassung or Tarifwechsel), until the next change.
/// </summary>
/// <param name="EffectiveFrom">First day the tariff applies. Price changes take effect at the start of a German calendar day.</param>
/// <param name="Tariff">The tariff in effect from that day.</param>
public sealed record TariffChange(DateOnly EffectiveFrom, Tariff Tariff);
