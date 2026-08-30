using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Extensions;
using Genbox.FastData.Generators.Helpers;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

/// <summary>
/// Rejects keys outside the observed [Min, Max] range using a single unsigned subtraction check for integral types,
/// or an <c>OrElse</c> comparison for floating-point types.
/// </summary>
/// <remarks>
/// For integral types the expression is <c>(unsigned)(key - Min) &gt; (unsigned)(Max - Min)</c>.
/// For floating-point types the expression is <c>key &lt; Min || key &gt; Max</c>.
/// </remarks>
/// <typeparam name="T">The key type.</typeparam>
/// <param name="Min">The inclusive minimum accepted value.</param>
/// <param name="Max">The inclusive maximum accepted value.</param>
/// <param name="LessThanKeyspace">The number or estimate of values below <paramref name="Min" />.</param>
/// <param name="GreaterThanKeyspace">The number or estimate of values above <paramref name="Max" />.</param>
public sealed record ValueOutOfRangeEarlyExit<T>(T Min, T Max, ulong LessThanKeyspace, ulong GreaterThanKeyspace) : IEarlyExit
{
    /// <inheritdoc />
    public ulong KeyspaceSize => LessThanKeyspace + GreaterThanKeyspace;

    /// <inheritdoc />
    public Expression GetExpression(ParameterExpression key)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));

        TypeCode typeCode = Type.GetTypeCode(typeof(T));

        if (typeCode.IsIntegral())
            return BuildUnsignedSubtraction(key, typeCode);

        // Floating-point: fall back to OrElse comparison
        Expression min = Constant(Min, key.Type);
        Expression max = Constant(Max, key.Type);
        return OrElse(LessThan(key, min), GreaterThan(key, max));
    }

    /// <inheritdoc />
    public bool IsWorseThan(IEarlyExit other) => false;

    private BinaryExpression BuildUnsignedSubtraction(ParameterExpression key, TypeCode typeCode)
    {
        Func<T, ulong> toUlong = typeCode.GetUnsignedValueConverter<T>();
        ulong minVal = toUlong(Min);
        ulong maxVal = toUlong(Max);
        ulong rangeVal = unchecked(maxVal - minVal);
        (BinaryExpression diff, ConstantExpression range) = IntegralExpressionHelper.CreateUnsignedRange(key, typeCode, minVal, rangeVal);
        return GreaterThan(diff, range);
    }
}