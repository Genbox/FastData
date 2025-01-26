using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis.Genetic;

[DebuggerDisplay("{Offset},{Length}")]
[StructLayout(LayoutKind.Auto)]
internal struct StringSegment
{
    internal int Offset;
    internal int Length;
}