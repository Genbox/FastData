using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>Checks that a character position equals a specific value.</summary>
public sealed record CharEqualsEarlyExit(CharPosition Position, char Value) : IEarlyExit
{
    public Expression GetExpression(string keyName)
    {
        ParameterExpression key = Expression.Parameter(typeof(string), keyName);
        MemberExpression keyLength = Expression.Property(key, nameof(string.Length));
        Expression index = Position == CharPosition.First
            ? Expression.Constant(0)
            : Expression.Subtract(keyLength, Expression.Constant(1));
        IndexExpression valueChar = Expression.Property(key, "Chars", index);

        return Expression.NotEqual(valueChar, Expression.Constant(Value));
    }
}