using System.Linq.Expressions;
using Genbox.FastData.Abstracts;
using static System.Linq.Expressions.Expression;

namespace Genbox.FastData.Specs.Hash;

/// <summary>Hashes the entire string using DJB2 hash</summary>
public sealed record DefaultStringHash : IExpressionStringHash
{
    public HashFunc GetHashFunction() => BuildExpression().Compile();
    public Expression<HashFunc> BuildExpression() => ExpressionHashBuilder.BuildFull(Mixer, Avalanche);

    // (((hash << 5) | (hash >> 27)) + hash) ^ Read(data, offset)
    private static Expression Mixer(Expression hash, Expression read) =>
        Assign(hash, ExclusiveOr(Add(Or(LeftShift(hash, Constant(5)), RightShift(hash, Constant(27))), hash), read));

    // 352654597 + (hash * 1566083941)
    private static Expression Avalanche(Expression hash) =>
        Add(Constant(352654597ul), Multiply(hash, Constant(1566083941ul)));
}