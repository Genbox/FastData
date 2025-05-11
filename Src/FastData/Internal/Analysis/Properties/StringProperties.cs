using System.Runtime.InteropServices;
using Genbox.FastData.Internal.Analysis.Data;

namespace Genbox.FastData.Internal.Analysis.Properties;

[StructLayout(LayoutKind.Auto)]
internal record StringProperties(LengthData LengthData, DeltaData DeltaData, CharacterData CharacterData);