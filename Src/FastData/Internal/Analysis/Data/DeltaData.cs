using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis.Data;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct DeltaData(string Prefix, int[]? LeftMap, string Suffix, int[]? RightMap);