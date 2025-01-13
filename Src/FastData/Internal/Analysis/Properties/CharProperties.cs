using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis.Properties;

[StructLayout(LayoutKind.Auto)]
internal record struct CharProperties(char MinValue, char MaxValue);