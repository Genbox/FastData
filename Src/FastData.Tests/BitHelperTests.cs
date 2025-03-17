using static Genbox.FastData.Internal.Helpers.BitHelper;

namespace Genbox.FastData.Tests;

public class BitHelperTests
{
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