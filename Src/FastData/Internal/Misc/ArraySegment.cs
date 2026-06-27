using System.Diagnostics;
using System.Runtime.InteropServices;
using Genbox.FastData.Internal.Enums;

namespace Genbox.FastData.Internal.Misc;

//Note: if the length is -1, it means we don't constraint it

[DebuggerDisplay("{Offset},{Length},{Alignment}")]
[StructLayout(LayoutKind.Auto)]
internal readonly record struct ArraySegment(uint Offset, int Length, Alignment Alignment)
{
    internal int GetRequiredLength(int unitSize)
    {
        int bytes = Length == -1 ? (int)Offset : (int)Offset + Length;
        return ((bytes + unitSize) - 1) / unitSize;
    }

    public override string ToString() => $"{Offset}|{Length}|{Alignment}";
}