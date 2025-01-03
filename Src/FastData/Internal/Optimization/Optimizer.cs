using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Optimization.EarlyExitSpecs;
using Genbox.FastData.Internal.Optimization.StringSpecs;

namespace Genbox.FastData.Internal.Optimization;

internal static class Optimizer
{
    public static IEnumerable<IEarlyExitSpec> GetEarlyExits(StringProperties prop)
    {
        yield return new MinMaxLengthEarlyExitSpec(prop.MinStrLen, prop.MaxStrLen);
    }

    public static IEnumerable<IEarlyExitSpec> GetEarlyExits(ArrayProperties prop)
    {
        yield return new MinMaxLengthEarlyExitSpec(prop.MinLength, prop.MaxLength);
    }

    public static IEnumerable<IEarlyExitSpec> GetEarlyExits(IntegerProperties prop)
    {
        yield return new MinMaxValueEarlyExitSpec(prop.MinValue, prop.MaxValue);
    }
}