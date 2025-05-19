using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Genbox.FastData.Misc;

//Note: if the length is -1, it means we don't constraint it

[DebuggerDisplay("{Offset},{Length},{Alignment}")]
[StructLayout(LayoutKind.Auto)]
public readonly record struct ArraySegment(uint Offset, int Length, Alignment Alignment)
{
    public override string ToString() => $"{Offset}|{Length}|{Alignment}";
}