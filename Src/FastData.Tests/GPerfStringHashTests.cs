using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Exits;
using Genbox.FastData.Generators.StringHash;
using Genbox.FastData.Generators.StringHash.Framework;

namespace Genbox.FastData.Tests;

public class GPerfStringHashTests
{
    [Fact]
    [SuppressMessage("ReSharper", "UseUtf8StringLiteral")]
    public void UsesSelectedPositionsOnly()
    {
        int[] association = Enumerable.Range(0, 256).ToArray();
        GPerfStringHash hash = new GPerfStringHash(association, new int[256], [3, 0], 2, false);
        StringHashFunc func = hash.GetExpression().Compile();

        byte[] left = [10, 20, 30, 40];
        byte[] right = [10, 99, 88, 40];

        Assert.Equal(func(left, left.Length), func(right, right.Length));
    }

    [Fact]
    public void UsesLogicalLengthWhenGuardingPositions()
    {
        int[] association = Enumerable.Range(0, 256).ToArray();
        GPerfStringHash hash = new GPerfStringHash(association, new int[256], [3], 2, false);
        StringHashFunc func = hash.GetExpression().Compile();

        byte[] value = [10, 20, 30, 40];

        Assert.Equal(0UL, func(value, 2));
    }

    [Fact]
    public void UsesLastByteForLastPosition()
    {
        int[] association = Enumerable.Range(0, 256).ToArray();
        GPerfStringHash hash = new GPerfStringHash(association, new int[256], [-1], 1, false);
        StringHashFunc func = hash.GetExpression().Compile();

        byte[] value = [10, 20, 30, 40];

        Assert.Equal(40UL, func(value, value.Length));
    }

    [Fact]
    public void GetMandatoryExits_AsciiBytes_ReturnsAsciiOnlyExit()
    {
        GPerfStringHash hash = new GPerfStringHash(new int[256], new int[256], [0], 1, false, GeneratorEncoding.AsciiBytes);
        IEarlyExit[] exits = hash.GetMandatoryExits().ToArray();

        LengthLessThanEarlyExit length = Assert.IsType<LengthLessThanEarlyExit>(exits[0]);
        Assert.Equal(1, length.Value);
        Assert.IsType<IsAsciiOnlyEarlyExit>(exits[1]);
    }

    [Fact]
    public void GetMandatoryExits_SevenBit_ReturnsAsciiOnlyExit()
    {
        GPerfStringHash hash = new GPerfStringHash(new int[256], new int[256], [0], 1, false, GeneratorEncoding.Utf8Bytes, true);
        IEarlyExit[] exits = hash.GetMandatoryExits().ToArray();

        LengthLessThanEarlyExit length = Assert.IsType<LengthLessThanEarlyExit>(exits[0]);
        Assert.Equal(1, length.Value);
        Assert.IsType<IsAsciiOnlyEarlyExit>(exits[1]);
    }

    [Fact]
    public void GetMandatoryExits_NoAsciiGuard_ReturnsLengthExit()
    {
        GPerfStringHash hash = new GPerfStringHash(new int[256], new int[256], [0], 3, false);
        IEarlyExit exit = Assert.Single(hash.GetMandatoryExits());

        LengthLessThanEarlyExit length = Assert.IsType<LengthLessThanEarlyExit>(exit);
        Assert.Equal(3, length.Value);
    }

    [Fact]
    public void GetMandatoryExits_UsesConfiguredMandatoryMinLength()
    {
        GPerfStringHash hash = new GPerfStringHash(new int[256], new int[256], [0], 6, false, mandatoryMinLength: 2);
        IEarlyExit exit = Assert.Single(hash.GetMandatoryExits());

        LengthLessThanEarlyExit length = Assert.IsType<LengthLessThanEarlyExit>(exit);
        Assert.Equal(2, length.Value);
    }
}
