using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Tests;

public class SwitchingBitSetTests
{
    [Fact]
    public void Constructor_LengthMustBeGreaterThanZero()
    {
        Assert.Throws<InvalidOperationException>(() => new SwitchingBitSet(0, false));
    }

    [Fact]
    public void AddAndContains_WorkInBitsetMode()
    {
        SwitchingBitSet set = new SwitchingBitSet(32, false);

        Assert.True(set.Add(1));
        Assert.True(set.Contains(1));
        Assert.False(set.Contains(2));
        Assert.False(set.Add(1));
    }

    [Fact]
    public void OffByOneMode_TracksZeroIndex()
    {
        SwitchingBitSet set = new SwitchingBitSet(32, true);

        Assert.True(set.Add(0));
        Assert.True(set.Contains(0));
        Assert.False(set.Contains(1));
    }

    [Fact]
    public void Add_SwitchesToSetWhenExceedingMaxWords()
    {
        SwitchingBitSet set = new SwitchingBitSet(64, false, 1);

        Assert.True(set.Add(0));
        Assert.True(set.IsBitSet);

        Assert.True(set.Add(64));
        Assert.False(set.IsBitSet);
        Assert.True(set.Contains(0));
        Assert.True(set.Contains(64));
    }
}