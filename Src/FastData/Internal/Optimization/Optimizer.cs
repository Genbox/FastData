using Genbox.FastData.Abstracts;
using Genbox.FastData.EarlyExitSpecs;
using Genbox.FastData.Internal.Analysis.Misc;
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

        IntegerBitSet lengthMap = prop.LengthData.LengthMap;

        if (lengthMap.Consecutive)
            yield return new MinMaxLengthEarlyExit(lengthMap.MinValue, lengthMap.MaxValue); //Also handles same lengths
        else
            yield return new LengthBitSetEarlyExit(lengthMap.BitSet);
    }

    public static IEnumerable<IEarlyExit> GetEarlyExits(IntegerProperties prop)
    {
        yield return new MinMaxValueEarlyExit(prop.MinValue, prop.MaxValue);
    }

    public static IEnumerable<IEarlyExit> GetEarlyExits(UnsignedIntegerProperties prop)
    {
        yield return new MinMaxUnsignedValueEarlyExit(prop.MinValue, prop.MaxValue);
    }

    public static IEnumerable<IEarlyExit> GetEarlyExits(CharProperties prop)
    {
        yield return new MinMaxValueEarlyExit(prop.MinValue, prop.MaxValue);
    }

    public static IEnumerable<IEarlyExit> GetEarlyExits(FloatProperties prop)
    {
        yield return new MinMaxFloatValueEarlyExit(prop.MinValue, prop.MaxValue);
    }
}