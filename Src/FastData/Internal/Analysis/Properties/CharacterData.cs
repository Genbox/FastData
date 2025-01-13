using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal.Analysis.Properties;

[StructLayout(LayoutKind.Auto)]
internal record struct CharacterData(bool AllAscii, uint[] CharacterCounts, CharacterMap CharacterMap);