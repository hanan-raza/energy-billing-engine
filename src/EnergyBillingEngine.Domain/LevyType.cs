namespace EnergyBillingEngine.Domain;

/// <summary>Kind of per-kWh charge passed on to the customer on top of the energy price.</summary>
/// <remarks>Rates in this project are synthetic; only the structure follows the German market.</remarks>
public enum LevyType
{
    /// <summary>Network fee (Netzentgelt, energy component), charged by the grid operator.</summary>
    NetworkFee = 1,

    /// <summary>Concession fee (Konzessionsabgabe), also called franchise fee, paid to the municipality for using public roads.</summary>
    ConcessionFee = 2,

    /// <summary>Electricity tax (Stromsteuer).</summary>
    ElectricityTax = 3,

    /// <summary>Combined heat and power surcharge (KWKG-Umlage).</summary>
    ChpSurcharge = 4,

    /// <summary>Offshore grid surcharge (Offshore-Netzumlage).</summary>
    OffshoreGridSurcharge = 5,

    /// <summary>Renewable energy surcharge (EEG-Umlage). Zero since 1 July 2022; kept to bill older periods.</summary>
    EegSurcharge = 6,
}
