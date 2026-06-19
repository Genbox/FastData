using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Enums;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Analysis;

internal static class SegmentScorer
{
    [SuppressMessage("Performance", "MA0159:Use \'Order\' instead of \'OrderBy\'", Justification = ".NET Standard 2.0 does not have that method")]
    internal static IEnumerable<ArraySegment> Order(StringKeyProperties props, IEnumerable<ArraySegment> segments) => segments.OrderByDescending(x => x, new SegmentComparer(props));

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