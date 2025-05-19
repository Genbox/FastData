using System.Linq.Expressions;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Specs.Misc;
using Xunit;

namespace Genbox.FastData.InternalShared;

public sealed class ExpressionHashDataClass : TheoryData<HashType, IExpressionArrayHash>
{
    public ExpressionHashDataClass()
    {
        Add(HashType.Full, new DefaultArrayHash());
        Add(HashType.Full, new BruteForceArrayHash(new ArraySegment(0, -1, Alignment.Left), Mixer, Avalanche));
        Add(HashType.Partial, new BruteForceArrayHash(new ArraySegment(1, 4, Alignment.Left), Mixer, Avalanche));
    }

    private static Expression Avalanche(Expression hash) => hash;
    private static BinaryExpression Mixer(Expression hash, Expression readFunc) => Expression.Add(hash, readFunc);
}