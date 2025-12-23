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

    private static IEnumerable<ISegmentGenerator> GetGenerators() => [new BruteForceGenerator(8), new EdgeGramGenerator(8), new DeltaGenerator(), new OffsetGenerator()];
}