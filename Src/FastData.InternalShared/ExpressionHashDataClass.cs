using System.Linq.Expressions;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Generator.CSharp.Tests;
using Genbox.FastData.Specs.Hash;
using Genbox.FastData.Specs.Misc;
using Xunit;

namespace Genbox.FastData.InternalShared;

public sealed class ExpressionHashDataClass : TheoryData<HashType, IExpressionStringHash>
{
    public ExpressionHashDataClass()
    {
        Add(HashType.Full, new DefaultStringHash());
        Add(HashType.Full, new BruteForceStringHash(new StringSegment(0, -1, Alignment.Left), Mixer, Avalanche));
        Add(HashType.Partial, new BruteForceStringHash(new StringSegment(1, 4, Alignment.Left), Mixer, Avalanche));
    }

    private static Expression Avalanche(Expression hash) => hash;
    private static BinaryExpression Mixer(Expression hash, Expression readFunc) => Expression.Add(hash, readFunc);
}