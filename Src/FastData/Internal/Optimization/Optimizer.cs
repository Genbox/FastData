using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Optimization.EarlyExitSpecs;

namespace Genbox.FastData.Internal.Optimization;

internal static class Optimizer
{
    public static IEnumerable<IEarlyExit> GetEarlyExits(StringProperties prop)
    {
        yield return new MinMaxLengthEarlyExit(prop.MinStrLen, prop.MaxStrLen);
    }

    public static IEnumerable<IEarlyExit> GetEarlyExits(IntegerProperties prop)
    {
        yield return new MinMaxValueEarlyExit(prop.MinValue, prop.MaxValue);
    }
}