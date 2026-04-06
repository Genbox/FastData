using Genbox.FastData.Config.Limits;

namespace Genbox.FastData.Tests;

public class LimitTests
{
    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    [InlineData(10)]
    public void ValueMinMaxLimit_IsWithinLimit_CurrentlyAlwaysTrue(int value)
    {
        ValueMinMaxLimit<int> limit = new ValueMinMaxLimit<int>(2, 8);

        Assert.True(limit.IsWithinLimit(value));
    }

    [Theory]
    [InlineData(0.0f, true)]
    [InlineData(0.5f, true)]
    [InlineData(1.0f, true)]
    [InlineData(-0.01f, false)]
    [InlineData(1.01f, false)]
    public void ValueDensityMinMaxLimit_IsWithinLimit_UsesInclusiveBounds(float value, bool expected)
    {
        ValueDensityMinMaxLimit limit = new ValueDensityMinMaxLimit(0.0f, 1.0f);

        Assert.Equal(expected, limit.IsWithinLimit(value));
    }
}