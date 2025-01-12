using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Optimization.EarlyExitSpecs;

namespace Genbox.FastData.Internal.Optimization;

internal static class Optimizer
{
    public static IEnumerable<IEarlyExit> GetEarlyExits(StringProperties prop)
    {
        IntegerBitSet lengthMap = prop.LengthMap;

        yield return new MinMaxLengthEarlyExit(lengthMap.MinValue, lengthMap.MaxValue);
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