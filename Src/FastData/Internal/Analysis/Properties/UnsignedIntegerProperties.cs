using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis.Properties;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct UnsignedIntegerProperties<T>(T MinValue, T MaxValue, bool Consecutive);