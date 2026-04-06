using Genbox.FastData.Config.Limits;

namespace Genbox.FastData.Tests;

public class ItemCountMinMaxLimitTests
{
    [Theory]
    [InlineData(0u)]
    [InlineData(5u)]
    [InlineData(10u)]
    public void IsWithinLimit_CurrentlyAlwaysTrue(uint value)
    {
        ItemCountMinMaxLimit limit = new ItemCountMinMaxLimit(2, 8);

        Assert.True(limit.IsWithinLimit(value));
    }
}