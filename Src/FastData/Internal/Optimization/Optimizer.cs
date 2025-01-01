using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Optimization.EarlyExitSpecs;
using Genbox.FastData.Internal.Optimization.StringSpecs;

namespace Genbox.FastData.Internal.Optimization;

internal static class Optimizer
{
    public static IStringSpec GetOptimalSpec(AnalysisResult results)
    {
        return new FullStringSpec();
    }

    public static IEnumerable<IEarlyExitSpec> GetEarlyExitSpecs(AnalysisResult results)
    {
        StringProperties prop = results.StringProperties;

        //We handle the fact that min and max can be the same value in the code generator
        yield return new MinMaxLengthEarlyExitSpec(prop.MinStrLen, prop.MaxStrLen);
    }
}