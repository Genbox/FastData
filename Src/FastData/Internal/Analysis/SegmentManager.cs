using System.Diagnostics;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Misc;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Analysis.SegmentGenerators;

namespace Genbox.FastData.Internal.Analysis;

internal static class SegmentManager
{
    internal static IEnumerable<StringSegment> Generate(StringProperties props, IEnumerable<ISegmentGenerator> generators)
    {
        HashSet<StringSegment> uniq = new HashSet<StringSegment>();

        foreach (ISegmentGenerator generator in generators)
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

    internal static IEnumerable<ISegmentGenerator> GetGenerators()
    {
        //TODO: Crashes due to code analysis assembly

        // Find all segment generators with reflection
        // foreach (Type type in typeof(SegmentManager).Assembly.GetTypes())
        // {
        //     if (!type.IsClass)
        //         continue;
        //
        //     if (type.IsAbstract)
        //         continue;
        //
        //     if (type.BaseType == typeof(ISegmentGenerator))
        //         yield return (ISegmentGenerator)Activator.CreateInstance(type);
        // }

        return [new BruteForceGenerator(), new EdgeGramGenerator(), new DeltaGenerator(), new OffsetGenerator()];
    }
}