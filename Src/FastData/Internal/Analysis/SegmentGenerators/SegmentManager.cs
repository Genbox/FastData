using System.Diagnostics;
using Genbox.FastData.Config.Analysis;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Analysis.SegmentGenerators;

internal static class SegmentManager
{
    internal static IEnumerable<ArraySegment> Generate(StringKeyProperties props, SegmentGeneratorConfig config)
    {
        HashSet<ArraySegment> uniq = new HashSet<ArraySegment>();

        // Collect from every generator before yielding so cross-generator candidates can be ranked by data signal.
        foreach (ISegmentGenerator generator in GetGenerators(config))
        {
            if (!generator.IsAppropriate(props))
                continue;

            foreach (ArraySegment segment in generator.Generate(props))
            {
                Debug.Assert(segment.Length is -1 or >= 1); //Length must always be -1 (unconstrained) or more than 0

                //Only return unique segments
                uniq.Add(segment);
            }
        }

        foreach (ArraySegment segment in SegmentScorer.Order(props, uniq))
            yield return segment;
    }

    // Ordered by a mix of complexity and value. DeltaGenerator should produce the best results the fastest.
    private static IEnumerable<ISegmentGenerator> GetGenerators(SegmentGeneratorConfig config)
    {
        if (config.DeltaGeneratorConfig != null)
            yield return new DeltaGenerator(config.DeltaGeneratorConfig);

        if (config.EdgeGramGeneratorConfig != null)
            yield return new EdgeGramGenerator(config.EdgeGramGeneratorConfig);

        if (config.BruteForceGeneratorConfig != null)
            yield return new BruteForceGenerator(config.BruteForceGeneratorConfig);

        if (config.OffsetGeneratorConfig != null)
            yield return new OffsetGenerator(config.OffsetGeneratorConfig);
    }
}