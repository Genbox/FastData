using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Genbox.FastData.Internal.Enums;

namespace Genbox.FastData.Internal.Analysis.Misc;

//Note: if length is -1, it means we don't constraint it

[DebuggerDisplay("{Offset},{Length},{Alignment}")]
[StructLayout(LayoutKind.Auto)]
internal readonly record struct StringSegment(int Offset, int Length, Alignment Alignment);