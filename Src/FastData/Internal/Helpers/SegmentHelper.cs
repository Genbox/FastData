using System.Text;
using Genbox.FastData.Internal.Enums;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Helpers;

internal static class SegmentHelper
{
    /// <summary>Inserts [ and ] around the segment of a string. Used for debugging</summary>
    internal static string InsertSegmentBounds(string input, in ArraySegment segment)
    {
        StringBuilder sb = new StringBuilder(input);
        ConvertToOffsets(input.Length, in segment, out int start, out int end);

        sb.Insert(start, '[');
        sb.Insert(end + 1, ']');
        return sb.ToString();
    }

    internal static void ConvertToOffsets(int strLen, in ArraySegment segment, out int start, out int end)
    {
        if (segment.Alignment == Alignment.Left)
        {
            start = (int)segment.Offset;
            end = (int)(segment.Length == -1 ? strLen : segment.Offset + segment.Length);
        }
        else if (segment.Alignment == Alignment.Right)
        {
            start = (int)(strLen - segment.Offset - (segment.Length == -1 ? strLen - segment.Offset : segment.Length));
            end = (int)(strLen - segment.Offset);
        }
        else
            throw new InvalidOperationException("Invalid alignment");
    }

    internal static ReadOnlySpan<char> GetSpan(in ArraySegment segment, string input)
    {
        ConvertToOffsets(input.Length, segment, out int start, out int end);
        return input.AsSpan(start, end - start);
    }

    internal static string Print(ArraySegment[] segments)
    {
        return string.Join(", ", segments.Select(x => x.ToString()));
    }
}