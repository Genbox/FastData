using System.Numerics;
using Genbox.FastData.Internal;

namespace Genbox.FastData.Tests;

public class ValueHelperTests
{
    [Fact]
    public void GetCharMinMax_VectorLength_ReturnsExpectedValues()
    {
        char[] keys = new char[(Vector<ushort>.Count * 2) + 1];

        Array.Fill(keys, 'm');
        keys[Vector<ushort>.Count - 1] = char.MaxValue;
        keys[^1] = '\0';

        ValueHelper.GetCharMinMax(keys, out char min, out char max);

        Assert.Equal('\0', min);
        Assert.Equal(char.MaxValue, max);
    }

    [Fact]
    public void GetInt16MinMax_VectorLength_ReturnsExpectedValues()
    {
        short[] keys = new short[(Vector<short>.Count * 2) + 1];

        Array.Fill(keys, (short)42);
        keys[Vector<short>.Count - 1] = short.MaxValue;
        keys[^1] = short.MinValue;

        ValueHelper.GetInt16MinMax(keys, out short min, out short max);

        Assert.Equal(short.MinValue, min);
        Assert.Equal(short.MaxValue, max);
    }

    [Fact]
    public void GetUInt16MinMax_VectorLength_ReturnsExpectedValues()
    {
        ushort[] keys = new ushort[(Vector<ushort>.Count * 2) + 1];

        Array.Fill(keys, (ushort)42);
        keys[Vector<ushort>.Count - 1] = ushort.MaxValue;
        keys[^1] = ushort.MinValue;

        ValueHelper.GetUInt16MinMax(keys, out ushort min, out ushort max);

        Assert.Equal(ushort.MinValue, min);
        Assert.Equal(ushort.MaxValue, max);
    }

    [Fact]
    public void GetInt32MinMax_VectorLength_ReturnsExpectedValues()
    {
        int[] keys = new int[(Vector<int>.Count * 2) + 1];

        Array.Fill(keys, 42);
        keys[Vector<int>.Count - 1] = int.MaxValue;
        keys[^1] = int.MinValue;

        ValueHelper.GetInt32MinMax(keys, out int min, out int max);

        Assert.Equal(int.MinValue, min);
        Assert.Equal(int.MaxValue, max);
    }

    [Fact]
    public void GetUInt32MinMax_VectorLength_ReturnsExpectedValues()
    {
        uint[] keys = new uint[(Vector<uint>.Count * 2) + 1];

        Array.Fill(keys, 42U);
        keys[Vector<uint>.Count - 1] = uint.MaxValue;
        keys[^1] = uint.MinValue;

        ValueHelper.GetUInt32MinMax(keys, out uint min, out uint max);

        Assert.Equal(uint.MinValue, min);
        Assert.Equal(uint.MaxValue, max);
    }

    [Fact]
    public void GetInt64MinMax_VectorLength_ReturnsExpectedValues()
    {
        long[] keys = new long[(Vector<long>.Count * 2) + 1];

        Array.Fill(keys, 42L);
        keys[Vector<long>.Count - 1] = long.MaxValue;
        keys[^1] = long.MinValue;

        ValueHelper.GetInt64MinMax(keys, out long min, out long max);

        Assert.Equal(long.MinValue, min);
        Assert.Equal(long.MaxValue, max);
    }

    [Fact]
    public void GetUInt64MinMax_VectorLength_ReturnsExpectedValues()
    {
        ulong[] keys = new ulong[(Vector<ulong>.Count * 2) + 1];

        Array.Fill(keys, 42UL);
        keys[Vector<ulong>.Count - 1] = ulong.MaxValue;
        keys[^1] = ulong.MinValue;

        ValueHelper.GetUInt64MinMax(keys, out ulong min, out ulong max);

        Assert.Equal(ulong.MinValue, min);
        Assert.Equal(ulong.MaxValue, max);
    }
}