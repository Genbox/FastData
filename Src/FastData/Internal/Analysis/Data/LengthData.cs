using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis.Data;

[StructLayout(LayoutKind.Auto)]
internal record struct LengthData(bool UniqueLengths, LengthBitArray LengthMap, bool UniqueByteLengths, LengthBitArray ByteLengthMap);