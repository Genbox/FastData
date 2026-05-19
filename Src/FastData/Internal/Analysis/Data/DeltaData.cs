using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis.Data;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct DeltaData(int[]? LeftMap, int[]? RightMap);