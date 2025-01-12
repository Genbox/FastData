using Genbox.FastData.Internal.Compat;

namespace Genbox.FastData.Tests;

public class BitOperationsTest
{
    [Fact]
    public void LeadingZeroCountTest()
    {
        for (int i = 0; i < 64; i++)
            Assert.Equal((uint)i, 63 - BitOperations.LeadingZeroCount(1UL << i));

        // Additional check for 0 (edge case)
        Assert.Equal(64u, BitOperations.LeadingZeroCount(0UL));
    }

    [Fact]
    public void TrailingZeroCountTest()
    {
        for (int i = 0; i < 64; i++)
            Assert.Equal((uint)i, BitOperations.TrailingZeroCount(1UL << i));

        // Additional check for 0 (edge case)
        Assert.Equal(64u, BitOperations.TrailingZeroCount(0UL));
    }
}