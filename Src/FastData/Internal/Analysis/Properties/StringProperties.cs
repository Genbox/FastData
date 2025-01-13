using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis.Properties;

[StructLayout(LayoutKind.Auto)]
internal record struct StringProperties(LengthData LengthData, EntropyData EntropyData, CharacterData CharacterData);