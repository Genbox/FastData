using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>Uses a bitmask from the first bytes of the string to quickly reject non-matching keys.</summary>
public sealed record StringBitMaskEarlyExit(ulong Mask, int ByteCount) : IEarlyExit
{
    public Expression GetExpression(string keyName)
    {
        if (Mask == 0 || ByteCount <= 0)
            return Expression.Constant(false);

        int charCount = ByteCount / 2;
        if (charCount <= 0 || charCount > 4)
            return Expression.Constant(false);

        ParameterExpression key = Expression.Parameter(typeof(string), keyName);
        Expression first = BuildFirstExpression(key, charCount);
        Expression masked = Expression.And(first, Expression.Constant(Mask));

        return Expression.NotEqual(masked, Expression.Constant(0UL));
    }

    private static Expression BuildFirstExpression(Expression key, int charCount)
    {
        Expression char0 = GetChar(key, 0);
        if (charCount == 1)
            return char0;

        Expression char1 = Expression.LeftShift(GetChar(key, 1), Expression.Constant(16));
        if (charCount == 2)
            return Expression.Or(char0, char1);

        Expression char2 = Expression.LeftShift(GetChar(key, 2), Expression.Constant(32));
        if (charCount == 3)
            return Expression.Or(Expression.Or(char0, char1), char2);

        Expression char3 = Expression.LeftShift(GetChar(key, 3), Expression.Constant(48));
        return Expression.Or(Expression.Or(char0, char1), Expression.Or(char2, char3));
    }

    private static Expression GetChar(Expression key, int index) =>
        Expression.Convert(Expression.Property(key, "Chars", Expression.Constant(index)), typeof(ulong));
}