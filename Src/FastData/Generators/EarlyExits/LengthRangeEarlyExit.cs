using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>Represents an early exit strategy that checks if a value is within a specified length range.</summary>
/// <param name="MinLength">The minimum string length.</param>
/// <param name="MaxLength">The maximum string length.</param>
/// <param name="MinLength">The minimum byte count.</param>
/// <param name="MaxLength">The maximum byte count.</param>
public sealed record LengthRangeEarlyExit(uint MinLength, uint MaxLength, uint MinByteCount, uint MaxByteCount) : IEarlyExit
{
    public Expression GetExpression(string keyName)
    {
        ParameterExpression key = Expression.Parameter(typeof(string), keyName);
        MemberExpression keyLength = Expression.Property(key, nameof(string.Length));
        UnaryExpression lengthValue = Expression.Convert(keyLength, typeof(uint));

        Expression minCheck = Expression.LessThan(lengthValue, Expression.Constant(MinLength));
        Expression maxCheck = Expression.GreaterThan(lengthValue, Expression.Constant(MaxLength));
        return Expression.OrElse(minCheck, maxCheck);
    }
}