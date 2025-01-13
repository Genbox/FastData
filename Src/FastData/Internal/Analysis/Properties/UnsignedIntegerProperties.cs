using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis.Properties;

[StructLayout(LayoutKind.Auto)]
internal record struct UnsignedIntegerProperties(ulong MinValue, ulong MaxValue, bool Consecutive);