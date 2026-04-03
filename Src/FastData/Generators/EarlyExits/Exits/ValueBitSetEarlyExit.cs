using System.Linq.Expressions;
using System.Numerics;
using Genbox.FastData.Generators.Abstracts;
using static Genbox.FastData.Generators.Helpers.TypeHelper;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// inputKey >= Start && inputKey <= End && ((MissingBitSet & (1UL << (inputKey - Start))) != 0);
public sealed record ValueBitSetEarlyExit<T>(T Start, T End, ulong MissingBitSet) : IEarlyExit
{
    public Expression GetExpression(ParameterExpression key)
    {
        Type keyType = key.Type;
        Type unsignedType = GetUnsignedType(keyType);
        Expression start = Constant(Start, keyType);
        Expression end = Constant(End, keyType);
        Expression lower = GreaterThanOrEqual(key, start);
        Expression upper = LessThanOrEqual(key, end);
        Expression inRange = AndAlso(lower, upper);

        Expression keyValue = keyType == unsignedType ? key : Convert(key, unsignedType);
        Expression startValue = keyType == unsignedType ? start : Convert(start, unsignedType);
        Expression keyUlong = unsignedType == typeof(ulong) ? keyValue : Convert(keyValue, typeof(ulong));
        Expression startUlong = unsignedType == typeof(ulong) ? startValue : Convert(startValue, typeof(ulong));
        Expression offset = Subtract(keyUlong, startUlong);
        Expression shift = Convert(offset, typeof(int));
        Expression bit = LeftShift(Constant(1UL), shift);
        Expression masked = And(Constant(MissingBitSet), bit);
        Expression missing = NotEqual(masked, Constant(0UL));

        return AndAlso(inRange, missing);
    }

    public bool IsWorseThan(IEarlyExit other) => false;

    public ulong KeyspaceSize => (ulong)BitOperations.PopCount(MissingBitSet);
}