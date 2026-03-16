using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>Checks that a character position falls within an observed range.</summary>
public sealed record CharRangeEarlyExit(CharPosition Position, char Min, char Max) : IEarlyExit
{
    public Expression GetExpression(string keyName)
    {
        ParameterExpression key = Expression.Parameter(typeof(string), keyName);
        MemberExpression keyLength = Expression.Property(key, nameof(string.Length));
        Expression index = Position == CharPosition.First
            ? Expression.Constant(0)
            : Expression.Subtract(keyLength, Expression.Constant(1));
        IndexExpression valueChar = Expression.Property(key, "Chars", index);

        Expression minCheck = Expression.LessThan(valueChar, Expression.Constant(Min));
        Expression maxCheck = Expression.GreaterThan(valueChar, Expression.Constant(Max));
        return Expression.OrElse(minCheck, maxCheck);
    }
}