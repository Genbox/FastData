using System.Runtime.InteropServices;
using Genbox.FastData.Internal.Analysis.Misc;

namespace Genbox.FastData.Internal.Analysis.Data;

[StructLayout(LayoutKind.Auto)]
internal record struct LengthData(uint Min, uint Max, IntegerBitSet LengthMap);