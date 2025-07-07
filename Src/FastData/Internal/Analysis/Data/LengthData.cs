using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis.Data;

[StructLayout(LayoutKind.Auto)]
internal record struct LengthData(uint Min, uint Max, uint MinUtf8ByteCount, uint MaxUtf8ByteCount, uint MinUtf16ByteCount, uint MaxUtf16ByteCount, bool Unique, LengthBitArray LengthMap);