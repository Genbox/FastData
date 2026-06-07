using Genbox.FastData.Config.Limits;

namespace Genbox.FastData.Tests;

public class LimitTests
{
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    [InlineData(4, true)]
    [InlineData(8, true)]
    [InlineData(9, false)]
    public void ValueMinMaxLimit_IsWithinLimit_UsesInclusiveBounds(int value, bool expected)
    {
        ValueMinMaxLimit<int> limit = new ValueMinMaxLimit<int>(2, 8);

        Assert.Equal(expected, limit.IsWithinLimit(value));
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