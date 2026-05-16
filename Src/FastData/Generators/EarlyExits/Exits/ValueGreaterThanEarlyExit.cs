using System.Globalization;
using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Abstracts;
using Genbox.FastData.Generators.Extensions;
using Convert = System.Convert;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// inputKey > Value;
public sealed record ValueGreaterThanEarlyExit<T>(T Value) : ValueComparisonEarlyExitBase<T>(Value)
{
    public override ulong KeyspaceSize
    {
        get
        {
            // Rejected keyspace is all values greater than the threshold.
            TypeCode code = Type.GetTypeCode(typeof(T));
            if (code.IsIntegral())
            {
                // Convert into unsigned space; subtract values <= threshold from domain size.
                Func<T, ulong> conv = code.GetUnsignedValueConverter<T>();
                ulong min = conv(code.GetMinValue<T>());
                ulong val = conv(Value);
                ulong lessThan = unchecked(val - min);
                ulong lessOrEqual = unchecked(lessThan + 1);
                int bitWidth = code.GetBitWidth();

                if (bitWidth == 64)
                    return unchecked(0UL - lessOrEqual);

                ulong domainSize = 1UL << bitWidth;
                return domainSize > lessOrEqual ? domainSize - lessOrEqual : 0UL;
            }

            // Floating point is a heuristic based on numeric difference.
            double floatValue = Convert.ToDouble(Value, CultureInfo.InvariantCulture);
            double floatMax = Convert.ToDouble(code.GetMaxValue<T>(), CultureInfo.InvariantCulture);
            double diff = floatMax - floatValue;
            return ClampToUInt64(diff);
        }
    }

    protected override BinaryExpression Compare(Expression left, Expression right) => GreaterThan(left, right);

    public override bool IsWorseThan(IEarlyExit other) => other is ValueGreaterThanEarlyExit<T> otherExit && Comparer<T>.Default.Compare(Value, otherExit.Value) > 0;

    private static ulong ClampToUInt64(double value)
    {
        if (double.IsNaN(value) || value <= 0)
            return 0;

        if (value >= ulong.MaxValue || double.IsPositiveInfinity(value))
            return ulong.MaxValue;

        return (ulong)value;
    }
}