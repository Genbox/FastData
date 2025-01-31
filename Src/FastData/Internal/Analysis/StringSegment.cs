using System.Diagnostics;
using System.Runtime.InteropServices;
using Genbox.FastData.Internal.Enums;

namespace Genbox.FastData.Internal.Analysis;

[DebuggerDisplay("{Offset},{Length}")]
[StructLayout(LayoutKind.Auto)]
internal record struct StringSegment(int Offset, int Length, Alignment Alignment);