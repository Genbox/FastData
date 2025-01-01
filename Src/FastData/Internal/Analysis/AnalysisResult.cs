using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis;

[StructLayout(LayoutKind.Auto)]
internal record struct AnalysisResult(StringProperties StringProperties, int NumItems, byte LongestLeft, byte LongestRight);