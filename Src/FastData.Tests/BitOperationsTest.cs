using static Genbox.FastData.Internal.Compat.BitOperations;

namespace Genbox.FastData.Tests;

public class BitOperationsTest
{
    [Fact]
    public void LeadingZeroCountTest()
    {
        for (int i = 0; i < 64; i++)
            Assert.Equal((uint)i, 63 - LeadingZeroCount(1UL << i));

        // Additional check for 0 (edge case)
        Assert.Equal(64u, LeadingZeroCount(0UL));
    }

    [Fact]
    public void TrailingZeroCountTest()
    {
        for (int i = 0; i < 64; i++)
            Assert.Equal((uint)i, TrailingZeroCount(1UL << i));

        // Additional check for 0 (edge case)
        Assert.Equal(64u, TrailingZeroCount(0UL));
    }

    [Fact]
    public void AreBitsConsecutiveTest()
    {
        Assert.True(AreBitsConsecutive(0b00011100UL)); // Consecutive bits (3rd, 4th, 5th)
        Assert.True(AreBitsConsecutive(0b1UL)); // Single bit set
        Assert.False(AreBitsConsecutive(0b01011000UL)); // Non-consecutive bits
        Assert.False(AreBitsConsecutive(0UL)); // No bits set
        Assert.True(AreBitsConsecutive(0xFFFFFFFFFFFFFFFFUL)); // All bits set
        Assert.False(AreBitsConsecutive(0b100001UL)); // Non-consecutive bits
    }
}