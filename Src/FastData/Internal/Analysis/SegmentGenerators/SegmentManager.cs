using System.Diagnostics;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Analysis.SegmentGenerators;

internal static class SegmentManager
{
    internal static IEnumerable<ArraySegment> Generate(StringKeyProperties props)
    {
        HashSet<ArraySegment> uniq = new HashSet<ArraySegment>();

        foreach (ISegmentGenerator generator in GetGenerators())
        {
            if (!generator.IsAppropriate(props))
                continue;

            foreach (ArraySegment segment in generator.Generate(props))
            {
                Debug.Assert(segment.Length is -1 or >= 1); //Length must always be -1 (unconstrained) or more than 0

                //Only return unique segments
                if (uniq.Add(segment))
                    yield return segment;
            }
        }
    }

    // Ordered by a mix of complexity and value. DeltaGenerator should produce the best results the fastest.
    private static IEnumerable<ISegmentGenerator> GetGenerators() => [new DeltaGenerator(), new EdgeGramGenerator(8), new BruteForceGenerator(8), new OffsetGenerator()];
}