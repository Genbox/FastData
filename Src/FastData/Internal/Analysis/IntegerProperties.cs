using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis;

[StructLayout(LayoutKind.Auto)]
internal record struct IntegerProperties(long MinValue, long MaxValue, bool Consecutive);

[StructLayout(LayoutKind.Auto)]
internal record struct UnsignedIntegerProperties(ulong MinValue, ulong MaxValue, bool Consecutive);