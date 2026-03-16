using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>Checks a character position against an ASCII bitmap.</summary>
public sealed record CharBitmapEarlyExit(CharPosition Position, ulong Low, ulong High) : IEarlyExit
{
    public Expression GetExpression(string keyName)
    {
        ParameterExpression key = Expression.Parameter(typeof(string), keyName);
        MemberExpression keyLength = Expression.Property(key, nameof(string.Length));
        Expression index = Position == CharPosition.First
            ? Expression.Constant(0)
            : Expression.Subtract(keyLength, Expression.Constant(1));
        IndexExpression valueChar = Expression.Property(key, "Chars", index);
        UnaryExpression valueCharUInt = Expression.Convert(valueChar, typeof(uint));

        Expression greaterThanAscii = Expression.GreaterThan(valueCharUInt, Expression.Constant(0x7Fu));
        Expression lessThan64 = Expression.LessThan(valueCharUInt, Expression.Constant(64u));

        Expression lowShift = Expression.LeftShift(Expression.Constant(1UL), Expression.Convert(valueCharUInt, typeof(int)));
        Expression lowMasked = Expression.And(Expression.Constant(Low), lowShift);
        Expression lowInvalid = Expression.Equal(lowMasked, Expression.Constant(0UL));
        Expression lowCheck = Expression.AndAlso(lessThan64, lowInvalid);

        Expression highShift = Expression.LeftShift(Expression.Constant(1UL), Expression.Convert(Expression.Subtract(valueCharUInt, Expression.Constant(64u)), typeof(int)));
        Expression highMasked = Expression.And(Expression.Constant(High), highShift);
        Expression highInvalid = Expression.Equal(highMasked, Expression.Constant(0UL));
        Expression highCheck = Expression.AndAlso(Expression.GreaterThanOrEqual(valueCharUInt, Expression.Constant(64u)), highInvalid);

        return Expression.OrElse(greaterThanAscii, Expression.OrElse(lowCheck, highCheck));
    }
}