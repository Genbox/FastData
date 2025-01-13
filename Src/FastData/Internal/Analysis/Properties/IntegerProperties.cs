using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis.Properties;

[StructLayout(LayoutKind.Auto)]
internal record struct IntegerProperties(long MinValue, long MaxValue, bool Consecutive);