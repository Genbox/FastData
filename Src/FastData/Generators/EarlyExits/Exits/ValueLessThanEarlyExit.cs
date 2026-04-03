using System.Globalization;
using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Abstracts;
using Genbox.FastData.Generators.Extensions;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// inputKey < Value;
public sealed record ValueLessThanEarlyExit<T>(T Value) : ValueComparisonEarlyExitBase<T>(Value)
{
    protected override BinaryExpression Compare(Expression left, Expression right) => LessThan(left, right);

    public override bool IsWorseThan(IEarlyExit other) => other is ValueLessThanEarlyExit<T> otherExit && Comparer<T>.Default.Compare(Value, otherExit.Value) < 0;

    public override ulong KeyspaceSize
    {
        get
        {
            // Rejected keyspace is all values less than the threshold.
            TypeCode code = Type.GetTypeCode(typeof(T));
            if (code.IsIntegral())
            {
                // Convert into unsigned space for ordering; difference from min gives count.
                Func<T, ulong> conv = code.GetUnsignedValueConverter<T>();
                ulong min = conv(code.GetMinValue<T>());
                ulong val = conv(Value);
                return unchecked(val - min);
            }

            // Floating point is a heuristic based on numeric difference.
            double floatValue = System.Convert.ToDouble(Value, CultureInfo.InvariantCulture);
            double floatMin = System.Convert.ToDouble(code.GetMinValue<T>(), CultureInfo.InvariantCulture);
            double diff = floatValue - floatMin;
            return ClampToUInt64(diff);
        }
    }

    private static ulong ClampToUInt64(double value)
    {
        if (double.IsNaN(value) || value <= 0)
            return 0;

        if (value >= ulong.MaxValue || double.IsPositiveInfinity(value))
            return ulong.MaxValue;

        return (ulong)value;
    }
}