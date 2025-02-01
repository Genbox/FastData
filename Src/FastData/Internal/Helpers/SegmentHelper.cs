using Genbox.FastData.Internal.Analysis.Misc;
using Genbox.FastData.Internal.Enums;

namespace Genbox.FastData.Internal.Helpers;

internal static class SegmentHelper
{
    /// <summary>Given a segment and a span, it returns a span containing the segment</summary>
    internal static ReadOnlySpan<char> GetString(ReadOnlySpan<char> input, in StringSegment segment)
    {
        GetBounds(input, in segment, out int start, out int end);
        return input.Slice(start, end);
    }

    /// <summary>Inserts [ and ] around the segment of a string. Used for debugging</summary>
    internal static string InsertSegmentBounds(ReadOnlySpan<char> input, in StringSegment segment)
    {
        List<char> lst = new List<char>(input.ToArray());
        GetBounds(input, in segment, out int start, out int end);

        lst.Insert(start, '[');
        lst.Insert(end + 1, ']');
        return new string(lst.ToArray());
    }

    internal static void GetBounds(ReadOnlySpan<char> input, in StringSegment segment, out int start, out int end)
    {
        if (segment.Alignment == Alignment.Left)
        {
            start = segment.Offset;
            end = segment.Length == -1 ? input.Length : segment.Offset + segment.Length;
        }
        else if (segment.Alignment == Alignment.Right)
        {
            start = input.Length - segment.Offset - segment.Length;
            end = segment.Length == -1 ? input.Length : input.Length - segment.Offset;
        }
        else
            throw new InvalidOperationException("Invalid alignment");
    }
}