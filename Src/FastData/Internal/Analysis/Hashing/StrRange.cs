using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis.Hashing;

[StructLayout(LayoutKind.Auto)]
internal record struct StrRange(uint Offset, uint Length);