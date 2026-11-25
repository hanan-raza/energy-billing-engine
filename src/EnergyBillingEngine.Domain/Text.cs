namespace EnergyBillingEngine.Domain;

/// <summary>Guard for the required text fields of the domain model.</summary>
internal static class Text
{
    /// <summary>Returns the trimmed value, or throws if it is empty or longer than <paramref name="maxLength"/>.</summary>
    public static string Required(string? value, int maxLength, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException($"Must not be longer than {maxLength} characters.", parameterName);
        }

        return trimmed;
    }
}
