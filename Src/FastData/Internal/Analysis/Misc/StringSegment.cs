using System.Diagnostics;
using System.Runtime.InteropServices;
using Genbox.FastData.Internal.Enums;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Internal.Analysis.Misc;

//Note: if length is -1, it means we don't constraint it

[DebuggerDisplay("{Offset},{Length},{Alignment}")]
[StructLayout(LayoutKind.Auto)]
internal readonly record struct StringSegment(int Offset, int Length, Alignment Alignment)
{
    internal ReadOnlySpan<char> GetSpan(string s)
    {
        SegmentHelper.ConvertToOffsets(s.Length, this, out int offset1, out int offset2);
        return s.AsSpan(offset1, offset2 - offset1);
    }
}