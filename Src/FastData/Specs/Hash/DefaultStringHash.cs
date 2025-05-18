using System.Linq.Expressions;
using Genbox.FastData.Abstracts;

namespace Genbox.FastData.Specs.Hash;

/// <summary>Hashes the entire string using DJB2 hash</summary>
public sealed record DefaultStringHash : IExpressionStringHash
{
    public HashFunc GetHashFunction() => BuildExpression().Compile();
    public Expression<HashFunc> BuildExpression() => ExpressionHashBuilder.BuildFull(Mixer, Avalanche);

    private static Expression Mixer(Expression hash, Expression read)
    {
        // (((hash << 5) | (hash >> 27)) + hash) ^ Read(data, offset)
        BinaryExpression rotated = Expression.Or(Expression.LeftShift(hash, Expression.Constant(5)), Expression.RightShift(hash, Expression.Constant(27)));
        return Expression.Assign(hash, Expression.ExclusiveOr(Expression.Add(rotated, hash), read));
    }

    private static Expression Avalanche(Expression hash)
    {
        // 352654597 + (hash * 1566083941)
        return Expression.Add(Expression.Constant(352654597ul), Expression.Multiply(hash, Expression.Constant(1566083941ul)));
    }
}