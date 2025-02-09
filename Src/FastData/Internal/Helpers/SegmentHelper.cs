using System.Text;
using Genbox.FastData.Internal.Analysis.Misc;
using Genbox.FastData.Internal.Enums;

namespace Genbox.FastData.Internal.Helpers;

internal static class SegmentHelper
{
    /// <summary>Inserts [ and ] around the segment of a string. Used for debugging</summary>
    internal static string InsertSegmentBounds(string input, in StringSegment segment)
    {
        StringBuilder sb = new StringBuilder(input);
        ConvertToOffsets(input.Length, in segment, out int start, out int end);

        sb.Insert(start, '[');
        sb.Insert(end + 1, ']');
        return sb.ToString();
    }

    internal static void ConvertToOffsets(int strLen, in StringSegment segment, out int start, out int end)
    {
        if (segment.Alignment == Alignment.Left)
        {
            start = segment.Offset;
            end = segment.Length == -1 ? strLen : segment.Offset + segment.Length;
        }
        else if (segment.Alignment == Alignment.Right)
        {
            start = strLen - segment.Offset - (segment.Length == -1 ? strLen - segment.Offset : segment.Length);
            end = strLen - segment.Offset;
        }
        else
            throw new InvalidOperationException("Invalid alignment");
    }
}