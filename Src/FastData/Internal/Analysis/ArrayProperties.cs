using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis;

[StructLayout(LayoutKind.Auto)]
internal record struct ArrayProperties(uint MinLength, uint MaxLength, uint NumLengths);