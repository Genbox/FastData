using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Config.Analysis;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Enums;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Analysis.SegmentGenerators;

internal static class SegmentManager
{
    [SuppressMessage("Performance", "MA0159:Use \'Order\' instead of \'OrderBy\'", Justification = ".NET Standard 2.0 does not have that method")]
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

        foreach (ArraySegment segment in uniq.OrderByDescending(x => x, new SegmentComparer(props)))
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

    private sealed class SegmentComparer(StringKeyProperties props) : IComparer<ArraySegment>
    {
        public int Compare(ArraySegment x, ArraySegment y)
        {
            int scoreCompare = GetDeltaScore(y).CompareTo(GetDeltaScore(x));
            if (scoreCompare != 0)
                return scoreCompare;

            int lengthCompare = GetEffectiveLength(x).CompareTo(GetEffectiveLength(y));
            if (lengthCompare != 0)
                return lengthCompare;

            int alignmentCompare = GetAlignmentCost(x.Alignment).CompareTo(GetAlignmentCost(y.Alignment));
            if (alignmentCompare != 0)
                return alignmentCompare;

            int offsetCompare = x.Offset.CompareTo(y.Offset);
            if (offsetCompare != 0)
                return offsetCompare;

            return x.Length.CompareTo(y.Length);
        }

        private int GetDeltaScore(ArraySegment segment)
        {
            int[]? map = segment.Alignment == Alignment.Right ? props.DeltaData.RightMap : props.DeltaData.LeftMap;
            if (map == null || map.Length == 0)
                return 0;

            int start = (int)segment.Offset;
            if (start >= map.Length)
                return 0;

            int end = Math.Min(map.Length, start + GetEffectiveLength(segment));
            int score = 0;
            int length = 0;

            for (int i = start; i < end; i++)
            {
                length++;
                int value = map[i];
                if (value != 0)
                    score += 256 + value;
            }

            // Normalize by length so a dense one-byte signal can outrank a long segment with diluted signal.
            return length == 0 ? 0 : score / length;
        }

        private int GetEffectiveLength(ArraySegment segment)
        {
            if (segment.Length != -1)
                return segment.Length;

            int length = props.LengthData.MinByteLength - (int)segment.Offset;
            return Math.Max(1, length);
        }

        private static int GetAlignmentCost(Alignment alignment) => alignment == Alignment.Left ? 0 : 1;
    }
}