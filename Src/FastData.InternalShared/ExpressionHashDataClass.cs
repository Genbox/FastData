using System.Linq.Expressions;
using Genbox.FastData.Abstracts;
using Genbox.FastData.ArrayHash;
using Genbox.FastData.Misc;
using Xunit;

namespace Genbox.FastData.InternalShared;

/// <summary>This class is used to generate expression-based hashes in each language to check if they are correctly outputting the expression.</summary>
public sealed class ExpressionHashDataClass : TheoryData<HashType, IStringHash>
{
    public ExpressionHashDataClass()
    {
        Add(HashType.Full, new DefaultStringHash());
        Add(HashType.Full, new BruteForceStringHash(new ArraySegment(0, -1, Alignment.Left), Mixer, Avalanche));
        Add(HashType.Partial, new BruteForceStringHash(new ArraySegment(1, 4, Alignment.Left), Mixer, Avalanche));
        Add(HashType.Full, new GeneticStringHash(new ArraySegment(0, -1, Alignment.Left), 1, 3, 10, 3));
        Add(HashType.Partial, new GeneticStringHash(new ArraySegment(1, 4, Alignment.Left), 1, 3, 10, 3));
        Add(HashType.Full, new GPerfStringHash(new int[255], new int[255], [1, 2, -1], 10));
    }

    private static Expression Avalanche(Expression hash) => hash;
    private static BinaryExpression Mixer(Expression hash, Expression readFunc) => Expression.Add(hash, readFunc);
}