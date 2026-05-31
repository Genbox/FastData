using System.Linq.Expressions;
using Genbox.FastData.Generators.StringHash.Framework;
using Genbox.FastData.Internal.Analysis.Expressions;
using Genbox.FastData.Internal.Enums;
using ArraySegment = Genbox.FastData.Internal.Misc.ArraySegment;

namespace Genbox.FastData.Tests;

public class ExpressionHashBuilderTests
{
    [Fact]
    public void Build_RightAlignedSegmentReadsFromEndOfInput()
    {
        StringHashFunc func = ExpressionHashBuilder.Build([new ArraySegment(0, 1, Alignment.Right)], static (_, read) => Expression.Convert(read, typeof(ulong)), static hash => hash).Compile();

        Assert.Equal(func([1, 2], 2), func([9, 2], 2));
        Assert.NotEqual(func([1, 2], 2), func([1, 9], 2));
    }

    [Fact]
    public void Build_FullTailUsesInitialSeedForEmptyInput()
    {
        const ulong seed = 17UL;
        StringHashFunc func = ExpressionHashBuilder.Build([new ArraySegment(0, -1, Alignment.Left)], MixExpression, static hash => hash, initialSeed: seed).Compile();

        Assert.Equal(seed, func([], 0));
    }

    [Fact]
    public void Build_FullTailProcessesLargeInputsThroughParallelLanes()
    {
        const int startOffset = 3;
        const ulong seed = 17UL;
        byte[] data = new byte[45];

        for (int i = 0; i < data.Length; i++)
            data[i] = (byte)(i + 1);

        StringHashFunc func = ExpressionHashBuilder.Build([new ArraySegment(startOffset, -1, Alignment.Left)], MixExpression, static hash => hash, initialSeed: seed).Compile();

        Assert.Equal(15424232090363977065ul, func(data, data.Length));
    }

    private static Expression MixExpression(Expression hash, Expression read) => Expression.Add(Expression.Multiply(hash, Expression.Constant(131UL)), read);
}