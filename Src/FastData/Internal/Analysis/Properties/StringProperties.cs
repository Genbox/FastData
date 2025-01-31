using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis.Properties;

[StructLayout(LayoutKind.Auto)]
internal record struct StringProperties(LengthData LengthData, DeltaData DeltaData, CharacterData CharacterData);