using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Extensions;
using static Genbox.FastData.Generators.Helpers.TypeHelper;
using Convert = System.Convert;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

/// <summary>Rejects keys outside the observed [Min, Max] range using a single unsigned subtraction check for integral types,
/// or an <c>OrElse</c> comparison for floating-point types.</summary>
/// <remarks>For integral types the expression is <c>(unsigned)(key - Min) &gt; (unsigned)(Max - Min)</c>.
/// For floating-point types the expression is <c>key &lt; Min || key &gt; Max</c>.</remarks>
public sealed record ValueOutOfRangeEarlyExit<T>(T Min, T Max, ulong LessThanKeyspace, ulong GreaterThanKeyspace) : IEarlyExit
{
    public ulong KeyspaceSize => LessThanKeyspace + GreaterThanKeyspace;

    public Expression GetExpression(ParameterExpression key)
    {
        TypeCode typeCode = Type.GetTypeCode(typeof(T));

        if (typeCode.IsIntegral())
            return BuildUnsignedSubtraction(key, typeCode);

        // Floating-point: fall back to OrElse comparison
        Expression min = Constant(Min, key.Type);
        Expression max = Constant(Max, key.Type);
        return OrElse(LessThan(key, min), GreaterThan(key, max));
    }

    public bool IsWorseThan(IEarlyExit other) => false;

    private Expression BuildUnsignedSubtraction(ParameterExpression key, TypeCode typeCode)
    {
        Type keyType = key.Type;
        Type unsignedType = GetUnsignedType(keyType);

        // Pre-compute min and range as unsigned constants to avoid runtime signed-to-unsigned conversions
        // that may produce invalid code in some target languages (e.g., Rust cannot negate unsigned literals).
        Func<T, ulong> toUlong = typeCode.GetUnsignedValueConverter<T>();
        ulong minVal = toUlong(Min);
        ulong maxVal = toUlong(Max);
        ulong rangeVal = unchecked(maxVal - minVal);

        // Convert key to unsigned type
        Expression keyUnsigned = keyType == unsignedType ? key : Convert(key, unsignedType);

        // Use pre-computed unsigned min constant
        object minConst = ConvertValueToType(minVal, unsignedType);
        Expression minUnsigned = Constant(minConst, unsignedType);

        // (unsigned)(key) - unsignedMin
        Expression diff = Subtract(keyUnsigned, minUnsigned);

        // Pre-computed (unsigned)(max - min)
        object rangeConst = ConvertValueToType(rangeVal, unsignedType);
        Expression range = Constant(rangeConst, unsignedType);

        return GreaterThan(diff, range);
    }
}