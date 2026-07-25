using System.Linq.Expressions;
using System.Numerics;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Extensions;
using Genbox.FastData.Generators.Helpers;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// inputKey >= Start && inputKey <= End && ((MissingBitSet & (1UL << (inputKey - Start))) != 0);
public sealed record ValueBitSetEarlyExit<T>(T Start, T End, ulong MissingBitSet) : IEarlyExit
{
    public Expression GetExpression(ParameterExpression key)
    {
        TypeCode typeCode = Type.GetTypeCode(key.Type);
        Func<T, ulong> toUlong = typeCode.GetUnsignedValueConverter<T>();
        ulong startVal = toUlong(Start);
        ulong endVal = toUlong(End);
        (BinaryExpression diff, ConstantExpression range) = IntegralExpressionHelper.CreateUnsignedRange(key, typeCode, startVal, unchecked(endVal - startVal));
        Expression inRange = LessThanOrEqual(diff, range);
        Expression shift = Convert(diff, typeof(int));
        Expression bit = LeftShift(Constant(1UL), shift);
        Expression masked = And(Constant(MissingBitSet), bit);
        Expression missing = NotEqual(masked, Constant(0UL));

        return AndAlso(inRange, missing);
    }

    public bool IsWorseThan(IEarlyExit other) => false;

    public ulong KeyspaceSize => (ulong)BitOperations.PopCount(MissingBitSet);
}