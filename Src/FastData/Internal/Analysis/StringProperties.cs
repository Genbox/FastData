using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis;

[StructLayout(LayoutKind.Auto)]
internal record struct StringProperties(uint MinStrLen, uint MaxStrLen, ushort MinChar, ushort MaxChar, bool UniqLength);