using Genbox.FastData.Abstracts;
using Genbox.FastData.EarlyExits;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Optimization;

internal static class Optimizer
{
    public static IEnumerable<IEarlyExit> GetEarlyExits(StringProperties prop)
    {
        //Logic:
        // - If all lengths are the same, we check against that (1 inst)
        // - If lengths are consecutive (5, 6, 7, etc.) we do a range check (2 inst)
        // - If the lengths are non-consecutive (4, 9, 12, etc.) we use a small bitset (4 inst)

        if (prop.LengthData.Max <= 64 && !prop.LengthData.LengthMap.Consecutive)
            yield return new LengthBitSetEarlyExit(prop.LengthData.LengthMap.BitSet);
        else
            yield return new MinMaxLengthEarlyExit(prop.LengthData.Min, prop.LengthData.Max); //Also handles same lengths
    }

    public static IEnumerable<IEarlyExit> GetEarlyExits<T>(IHasMinMax<T> prop)
    {
        yield return new MinMaxValueEarlyExit<T>(prop.MinValue, prop.MaxValue);
    }
}