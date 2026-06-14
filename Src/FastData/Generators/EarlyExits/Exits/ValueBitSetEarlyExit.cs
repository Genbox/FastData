using System.Linq.Expressions;
using System.Numerics;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Extensions;
using static Genbox.FastData.Generators.Helpers.TypeHelper;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// inputKey >= Start && inputKey <= End && ((MissingBitSet & (1UL << (inputKey - Start))) != 0);
public sealed record ValueBitSetEarlyExit<T>(T Start, T End, ulong MissingBitSet) : IEarlyExit
{
    public Expression GetExpression(ParameterExpression key)
    {
        Type keyType = key.Type;
        Type unsignedType = GetUnsignedType(keyType);
        TypeCode typeCode = Type.GetTypeCode(keyType);
        Func<T, ulong> toUlong = typeCode.GetUnsignedValueConverter<T>();
        ulong startVal = toUlong(Start);
        ulong endVal = toUlong(End);

        Expression keyValue = keyType == unsignedType ? key : Convert(key, unsignedType);
        object startConst = ConvertValueToType(startVal, unsignedType);
        Expression start = Constant(startConst, unsignedType);
        Expression diff = Subtract(keyValue, start);
        object rangeConst = ConvertValueToType(unchecked(endVal - startVal), unsignedType);
        Expression range = Constant(rangeConst, unsignedType);
        Expression inRange = LessThanOrEqual(diff, range);

        Expression offset = unsignedType == typeof(ulong) ? diff : Convert(diff, typeof(ulong));
        Expression shift = Convert(offset, typeof(int));
        Expression bit = LeftShift(Constant(1UL), shift);
        Expression masked = And(Constant(MissingBitSet), bit);
        Expression missing = NotEqual(masked, Constant(0UL));

        return AndAlso(inRange, missing);
    }

    public bool IsWorseThan(IEarlyExit other) => false;

    public ulong KeyspaceSize => (ulong)BitOperations.PopCount(MissingBitSet);
}