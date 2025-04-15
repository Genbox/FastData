using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Genbox.FastData.Specs.Hash;

//Note: if length is -1, it means we don't constraint it

[DebuggerDisplay("{Offset},{Length},{Alignment}")]
[StructLayout(LayoutKind.Auto)]
public readonly record struct StringSegment(int Offset, int Length, Alignment Alignment);