using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis.Properties;

[StructLayout(LayoutKind.Auto)]
internal record struct LengthData(uint Min, uint Max, IntegerBitSet LengthMap);