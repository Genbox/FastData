using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis.Data;

[StructLayout(LayoutKind.Auto)]
internal record struct LengthData(bool UniqueLengths, DataRanges<int> LengthRanges, int MinCharLength, int MaxCharLength);