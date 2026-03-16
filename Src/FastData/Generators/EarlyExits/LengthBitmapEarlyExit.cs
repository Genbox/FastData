using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>Represents an early exit strategy that uses a bitset to quickly determine valid string lengths.</summary>
/// <param name="BitSet">A bit set where each bit represents a valid string length.</param>
public sealed record LengthBitmapEarlyExit(ulong[] BitSet) : IEarlyExit
{
    public Expression GetExpression(string keyName)
    {
        ParameterExpression key = Expression.Parameter(typeof(string), keyName);
        MemberExpression keyLength = Expression.Property(key, nameof(string.Length));
        Expression shift = Expression.And(Expression.Subtract(keyLength, Expression.Constant(1)), Expression.Constant(63));

        Expression BuildWordCheck(ulong word)
        {
            Expression shiftedBit = Expression.LeftShift(Expression.Constant(1UL), shift);
            Expression masked = Expression.And(Expression.Constant(word), shiftedBit);
            return Expression.Equal(masked, Expression.Constant(0UL));
        }

        if (BitSet.Length == 1)
            return BuildWordCheck(BitSet[0]);

        Expression index = Expression.RightShift(keyLength, Expression.Constant(6));
        Expression exitCondition = Expression.GreaterThanOrEqual(index, Expression.Constant(BitSet.Length));

        for (int i = 0; i < BitSet.Length; i++)
        {
            Expression indexCheck = Expression.Equal(index, Expression.Constant(i));
            Expression wordCheck = BuildWordCheck(BitSet[i]);
            Expression caseCheck = Expression.AndAlso(indexCheck, wordCheck);
            exitCondition = Expression.OrElse(exitCondition, caseCheck);
        }

        return exitCondition;
    }
}