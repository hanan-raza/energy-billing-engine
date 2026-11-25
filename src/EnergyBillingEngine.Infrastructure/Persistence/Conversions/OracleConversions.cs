using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace EnergyBillingEngine.Infrastructure.Persistence.Conversions;

/// <summary>
/// Oracle's EF Core provider has no native mapping for <see cref="DateOnly"/> and <see cref="TimeOnly"/>
/// and falls back to NVARCHAR2 text columns. Text dates compare as strings and take any garbage, so they
/// are mapped to the native types explicitly: DATE for days, INTERVAL DAY TO SECOND for times of day.
/// </summary>
internal static class OracleConversions
{
    public static void Apply(ModelConfigurationBuilder configuration)
    {
        configuration.Properties<DateOnly>()
            .HaveConversion<DateOnlyAsDateConverter>()
            .HaveColumnType("DATE");

        configuration.Properties<TimeOnly>()
            .HaveConversion<TimeOnlyAsIntervalConverter>()
            .HaveColumnType("INTERVAL DAY(0) TO SECOND(0)");
    }

    // Oracle DATE always carries a time; midnight is the day itself.
    private sealed class DateOnlyAsDateConverter() : ValueConverter<DateOnly, DateTime>(
        day => day.ToDateTime(TimeOnly.MinValue),
        value => DateOnly.FromDateTime(value));

    // Time-of-use bands start on whole minutes, so seconds precision without fractions is enough.
    private sealed class TimeOnlyAsIntervalConverter() : ValueConverter<TimeOnly, TimeSpan>(
        time => time.ToTimeSpan(),
        value => TimeOnly.FromTimeSpan(value));
}
