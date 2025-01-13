using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis.Properties;

[StructLayout(LayoutKind.Auto)]
internal record struct DataProperties(StringProperties? StringProps, IntegerProperties? IntProps, UnsignedIntegerProperties? UIntProps, CharProperties? CharProps, FloatProperties? FloatProps);