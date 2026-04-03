using System.Globalization;
using System.Linq.Expressions;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Extensions;

namespace Genbox.FastData.Generators.EarlyExits.Exits;

// inputKey > Min && inputKey < Max;
public sealed record ValueInRangeEarlyExit<T>(T Min, T Max) : IEarlyExit
{
    public Expression GetExpression(ParameterExpression key)
    {
        Expression min = Constant(Min, key.Type);
        Expression max = Constant(Max, key.Type);
        Expression lower = GreaterThan(key, min);
        Expression upper = LessThan(key, max);
        return AndAlso(lower, upper);
    }

    public bool IsWorseThan(IEarlyExit other)
    {
        if (other is not ValueInRangeEarlyExit<T> otherExit)
            return false;

        int minCompare = Comparer<T>.Default.Compare(Min, otherExit.Min);
        int maxCompare = Comparer<T>.Default.Compare(Max, otherExit.Max);

        if (minCompare == 0 && maxCompare == 0)
            return false;

        return minCompare <= 0 && maxCompare >= 0;
    }

    public ulong KeyspaceSize
    {
        get
        {
            // Rejected keyspace is the size of the exclusive range.
            TypeCode code = Type.GetTypeCode(typeof(T));
            if (code.IsIntegral())
            {
                // Convert into unsigned space for ordering; exclusive range subtracts one.
                Func<T, ulong> conv = code.GetUnsignedValueConverter<T>();
                ulong min = conv(Min);
                ulong max = conv(Max);
                return unchecked(max - min - 1);
            }

            // Floating point is a heuristic based on numeric difference.
            double floatMin = System.Convert.ToDouble(Min, CultureInfo.InvariantCulture);
            double floatMax = System.Convert.ToDouble(Max, CultureInfo.InvariantCulture);
            double diff = floatMax - floatMin;

            if (diff >= ulong.MaxValue)
                return ulong.MaxValue;

            return (ulong)diff;
        }
    }
}