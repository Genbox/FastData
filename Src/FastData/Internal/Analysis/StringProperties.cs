using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis;

[StructLayout(LayoutKind.Auto)]
internal record struct StringProperties(IntegerBitSet LengthMap, EntropyData EntropyData, CharacterMap CharacterMap);