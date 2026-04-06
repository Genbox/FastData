using Genbox.FastData.Config.Limits;

namespace Genbox.FastData.Tests;

public class StringLengthMinMaxLimitTests
{
    [Theory]
    [InlineData(0u)]
    [InlineData(4u)]
    [InlineData(10u)]
    public void IsWithinLimit_CurrentlyAlwaysTrue(uint value)
    {
        StringLengthMinMaxLimit limit = new StringLengthMinMaxLimit(2, 8);

        Assert.True(limit.IsWithinLimit(value));
    }
}