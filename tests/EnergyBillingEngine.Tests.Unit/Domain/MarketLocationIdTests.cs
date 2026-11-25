using EnergyBillingEngine.Domain;
using Shouldly;

namespace EnergyBillingEngine.Tests.Unit.Domain;

public sealed class MarketLocationIdTests
{
    [Theory]
    [InlineData("51238696781")]
    [InlineData("51238696799")]
    [InlineData("41373559241")]
    public void Ids_with_a_correct_check_digit_are_valid(string id)
    {
        MarketLocationId.IsValid(id).ShouldBeTrue();
    }

    [Theory]
    [InlineData("51238696782")]
    [InlineData("01238696781")]
    [InlineData("5123869678")]
    [InlineData("512386967810")]
    [InlineData("5123869678A")]
    [InlineData(null)]
    public void Wrong_check_digit_length_or_leading_zero_is_invalid(string? id)
    {
        MarketLocationId.IsValid(id).ShouldBeFalse();
    }
}
