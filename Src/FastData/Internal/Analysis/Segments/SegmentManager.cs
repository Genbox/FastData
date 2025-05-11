using System.Diagnostics;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Specs.Hash;
using Genbox.FastData.Specs.Misc;

namespace Genbox.FastData.Internal.Analysis.Segments;

internal static class SegmentManager
{
    internal static IEnumerable<StringSegment> Generate(StringProperties props)
    {
        HashSet<StringSegment> uniq = new HashSet<StringSegment>();

        foreach (ISegmentGenerator generator in GetGenerators())
        {
            // if (!generator.IsAppropriate(props))
            // continue;

            foreach (StringSegment segment in generator.Generate(props))
            {
                Debug.Assert(segment.Length is -1 or >= 1); //Length must always be -1 (unconstrained) or more than 0

                //Only return unique segments
                if (uniq.Add(segment))
                    yield return segment;
            }
        }
    }

    private static IEnumerable<ISegmentGenerator> GetGenerators() => [new BruteForceGenerator(), new EdgeGramGenerator(), new DeltaGenerator(), new OffsetGenerator()];
}