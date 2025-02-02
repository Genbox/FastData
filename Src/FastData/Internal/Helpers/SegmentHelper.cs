using Genbox.FastData.Internal.Analysis.Misc;
using Genbox.FastData.Internal.Enums;

namespace Genbox.FastData.Internal.Helpers;

internal static class SegmentHelper
{
    /// <summary>Inserts [ and ] around the segment of a string. Used for debugging</summary>
    internal static string InsertSegmentBounds(string input, in StringSegment segment)
    {
        List<char> lst = new List<char>(input.ToArray());
        ConvertToOffsets(input.Length, in segment, out int start, out int end);

        lst.Insert(start, '[');
        lst.Insert(end + 1, ']');
        return new string(lst.ToArray());
    }

    internal static void ConvertToOffsets(int strLen, in StringSegment segment, out int offset1, out int offset2)
    {
        if (segment.Alignment == Alignment.Left)
        {
            offset1 = segment.Offset;
            offset2 = segment.Length == -1 ? strLen : segment.Offset + segment.Length;
        }
        else if (segment.Alignment == Alignment.Right)
        {
            offset1 = strLen - segment.Offset - segment.Length;
            offset2 = segment.Length == -1 ? strLen : strLen - segment.Offset;
        }
        else
            throw new InvalidOperationException("Invalid alignment");
    }
}