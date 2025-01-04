using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis;

[StructLayout(LayoutKind.Auto)]
internal record struct StringProperties(uint MinLen, uint MaxLen, ushort MinChar, ushort MaxChar, byte LongestLeft, byte LongestRight, uint NumLengths);