using Genbox.FastData.Config.Limits;

namespace Genbox.FastData.Tests;

public class ItemCountMinMaxLimitTests
{
    [Theory]
    [InlineData(1u, false)]
    [InlineData(2u, true)]
    [InlineData(5u, true)]
    [InlineData(8u, true)]
    [InlineData(9u, false)]
    public void IsWithinLimit_UsesInclusiveBounds(uint value, bool expected)
    {
        ItemCountMinMaxLimit limit = new ItemCountMinMaxLimit(2, 8);

        Assert.Equal(expected, limit.IsWithinLimit(value));
    }
}