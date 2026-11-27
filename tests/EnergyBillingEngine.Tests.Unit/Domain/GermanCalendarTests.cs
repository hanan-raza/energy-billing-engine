using EnergyBillingEngine.Domain;
using Shouldly;

namespace EnergyBillingEngine.Tests.Unit.Domain;

public sealed class GermanCalendarTests
{
    [Theory]
    [InlineData(2026, 3, 29, 23)]
    [InlineData(2026, 10, 25, 25)]
    [InlineData(2026, 3, 28, 24)]
    [InlineData(2026, 6, 15, 24)]
    [InlineData(2027, 3, 28, 23)]
    [InlineData(2027, 10, 31, 25)]
    public void Days_last_23_24_or_25_hours(int year, int month, int day, int hours)
    {
        GermanCalendar.LengthOf(new DateOnly(year, month, day)).ShouldBe(TimeSpan.FromHours(hours));
    }

    [Fact]
    public void Midnight_carries_the_offset_of_winter_or_summer_time()
    {
        GermanCalendar.StartOfDay(new DateOnly(2026, 1, 15)).Offset.ShouldBe(TimeSpan.FromHours(1));
        GermanCalendar.StartOfDay(new DateOnly(2026, 7, 15)).Offset.ShouldBe(TimeSpan.FromHours(2));
    }
}
