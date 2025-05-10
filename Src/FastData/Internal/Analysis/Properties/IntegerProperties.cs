using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis.Properties;

[StructLayout(LayoutKind.Auto)]
internal readonly record struct IntegerProperties<T>(T MinValue, T MaxValue, bool Consecutive);