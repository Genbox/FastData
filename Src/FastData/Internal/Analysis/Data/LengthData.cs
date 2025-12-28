using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis.Data;

[StructLayout(LayoutKind.Auto)]
internal record struct LengthData(uint MinByteCount, uint MaxByteCount, bool Unique, LengthBitArray LengthMap);