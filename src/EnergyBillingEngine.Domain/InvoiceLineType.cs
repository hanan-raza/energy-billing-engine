namespace EnergyBillingEngine.Domain;

/// <summary>What an invoice line charges for.</summary>
public enum InvoiceLineType
{
    /// <summary>Base price (Grundpreis), pro rata for the days supplied.</summary>
    BasePrice = 1,

    /// <summary>Energy price (Arbeitspreis) for the consumed kWh.</summary>
    Energy = 2,

    /// <summary>A levy, tax or network charge (Umlage, Abgabe, Netzentgelt).</summary>
    Levy = 3,

    /// <summary>Value added tax (Umsatzsteuer).</summary>
    Vat = 4,
}
