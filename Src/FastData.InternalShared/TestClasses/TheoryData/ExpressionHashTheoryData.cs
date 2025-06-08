using System.Linq.Expressions;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.StringHash;
using Genbox.FastData.Internal.Enums;
using Genbox.FastData.Internal.Misc;
using Xunit;

namespace Genbox.FastData.InternalShared.TestClasses.TheoryData;

/// <summary>This class is used to generate expression-based hashes in each language to check if they are correctly outputting the expression.</summary>
public class ExpressionHashTheoryData : TheoryData<HashType, Expression<HashFunc<string>>>
{
    public ExpressionHashTheoryData()
    {
        Add(HashType.Full, new DefaultStringHash().GetExpression());
        Add(HashType.Full, new BruteForceStringHash(new ArraySegment(0, -1, Alignment.Left), Mixer, Avalanche).GetExpression());
        Add(HashType.Partial, new BruteForceStringHash(new ArraySegment(1, 4, Alignment.Left), Mixer, Avalanche).GetExpression());
        Add(HashType.Full, new GeneticStringHash(new ArraySegment(0, -1, Alignment.Left), 1, 3, 10, 3).GetExpression());
        Add(HashType.Partial, new GeneticStringHash(new ArraySegment(1, 4, Alignment.Left), 1, 3, 10, 3).GetExpression());
        Add(HashType.Full, new GPerfStringHash(new int[255], new int[255], [1, 2, -1], 10).GetExpression());
    }

    private static Expression Avalanche(Expression hash) => hash;
    private static BinaryExpression Mixer(Expression hash, Expression readFunc) => Expression.Add(hash, readFunc);
}