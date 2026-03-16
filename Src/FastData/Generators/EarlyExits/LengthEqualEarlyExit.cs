using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>Represents an early exit strategy that checks if a value matches a specific length.</summary>
/// <param name="Length">The string length.</param>
/// <param name="ByteCount">The byte count.</param>
public sealed record LengthEqualEarlyExit(uint Length, uint ByteCount) : IEarlyExit
{
    public Expression GetExpression(string keyName)
    {
        ParameterExpression key = Expression.Parameter(typeof(string), keyName);
        MemberExpression keyLength = Expression.Property(key, nameof(string.Length));
        UnaryExpression lengthValue = Expression.Convert(keyLength, typeof(uint));

        return Expression.NotEqual(lengthValue, Expression.Constant(Length));
    }
}