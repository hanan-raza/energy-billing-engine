namespace EnergyBillingEngine.Domain;

/// <summary>Market location ID (Marktlokations-ID, MaLo-ID): 11 digits, the last one a check digit.</summary>
public static class MarketLocationId
{
    /// <summary>Number of characters.</summary>
    public const int Length = 11;

    /// <summary>True for 11 digits that do not start with 0 and end in the correct check digit.</summary>
    public static bool IsValid(string? value)
    {
        if (value is null || value.Length != Length || value[0] == '0' || !value.All(char.IsAsciiDigit))
        {
            return false;
        }

        return value[^1] - '0' == CalculateCheckDigit(value.AsSpan(0, Length - 1));
    }

    /// <summary>
    /// Digits at odd positions count once, digits at even positions count twice; the check digit
    /// tops the sum up to the next multiple of ten.
    /// </summary>
    /// <exception cref="ArgumentException">Not exactly ten characters.</exception>
    public static int CalculateCheckDigit(ReadOnlySpan<char> firstTenDigits)
    {
        if (firstTenDigits.Length != Length - 1)
        {
            throw new ArgumentException($"Expected {Length - 1} digits.", nameof(firstTenDigits));
        }

        var sum = 0;
        for (var i = 0; i < firstTenDigits.Length; i++)
        {
            var digit = firstTenDigits[i] - '0';
            sum += i % 2 == 0 ? digit : digit * 2;
        }

        return (10 - sum % 10) % 10;
    }
}
