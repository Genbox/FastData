using System.Diagnostics.CodeAnalysis;
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
        GPerfStringHash hash = new GPerfStringHash(association, new int[256], [3, 0], 2);
        StringHashFunc func = hash.GetExpression().Compile();

        byte[] left = [10, 20, 30, 40];
        byte[] right = [10, 99, 88, 40];

        Assert.Equal(func(left, left.Length), func(right, right.Length));
    }

    [Fact]
    public void UsesLogicalLengthWhenGuardingPositions()
    {
        int[] association = Enumerable.Range(0, 256).ToArray();
        GPerfStringHash hash = new GPerfStringHash(association, new int[256], [3], 2);
        StringHashFunc func = hash.GetExpression().Compile();

        byte[] value = [10, 20, 30, 40];

        Assert.Equal(0UL, func(value, 2));
    }
}