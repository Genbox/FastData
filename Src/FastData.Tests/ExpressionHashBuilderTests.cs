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
}