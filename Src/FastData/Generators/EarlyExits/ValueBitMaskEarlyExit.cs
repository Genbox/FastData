using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using static Genbox.FastData.Generators.Helpers.TypeHelper;

namespace Genbox.FastData.Generators.EarlyExits;

/// <summary>Represents an early exit strategy that checks for bits that can never be set.</summary>
public sealed record ValueBitMaskEarlyExit<TKey> : IEarlyExit
{
    private readonly ulong _mask;

    public ValueBitMaskEarlyExit(ulong mask)
    {
        _mask = mask;
    }

    public Expression GetExpression(string keyName)
    {
        Type KeyType = typeof(TKey);

        Type unsignedType = GetUnsignedType(KeyType);
        ParameterExpression key = Expression.Parameter(KeyType, keyName);
        Expression keyValue = KeyType == unsignedType ? key : Expression.Convert(key, unsignedType);
        object maskValue = ConvertValueToType(_mask, unsignedType);
        Expression masked = Expression.And(keyValue, Expression.Constant(maskValue, unsignedType));
        object zeroValue = ConvertValueToType(0, unsignedType);

        return Expression.NotEqual(masked, Expression.Constant(zeroValue, unsignedType));
    }

    public void Deconstruct(out ulong mask) => mask = _mask;
}