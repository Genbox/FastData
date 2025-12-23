using Genbox.FastData.Internal.Analysis.Data;

namespace Genbox.FastData.Tests;

public class LengthBitArrayTests
{
    [Fact]
    public void Constructor_ZeroLength_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => new LengthBitArray(0));
    }

    [Fact]
    public void DefaultBitsAreFalse()
    {
        LengthBitArray bits = new LengthBitArray();
        Assert.False(bits.Get(0));
        Assert.False(bits.Get(63));
    }

    [Fact]
    public void CustomLength_AllBitsFalseWithinRange()
    {
        LengthBitArray bits = new LengthBitArray(128);
        for (uint i = 0; i < 128; i += 15)
            Assert.False(bits.Get(i));
    }

    [Fact]
    public void SetTrue_SetsBitAndReturnsCorrectFlag()
    {
        LengthBitArray bits = new LengthBitArray();
        Assert.False(bits.SetTrue(10));
        Assert.True(bits.Get(10));

        bool second = bits.SetTrue(10);
        Assert.True(second);
        Assert.True(bits.Get(10));
    }

    [Fact]
    public void Expand_SetsBitBeyondInitialLength()
    {
        LengthBitArray bits = new LengthBitArray(10);
        bool wasSet = bits.SetTrue(20);
        Assert.False(wasSet);
        Assert.True(bits.Get(20));
    }

    [Fact]
    public void Expand_PreservesExistingBits()
    {
        LengthBitArray bits = new LengthBitArray(10);
        bits.SetTrue(2);
        bits.SetTrue(20);
        Assert.True(bits.Get(2));
        Assert.True(bits.Get(20));
    }
}