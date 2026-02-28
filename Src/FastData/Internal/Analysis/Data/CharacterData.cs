using System.Runtime.InteropServices;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Enums;

namespace Genbox.FastData.Internal.Analysis.Data;

[StructLayout(LayoutKind.Auto)]
internal record struct CharacterData(bool AllAscii, CharacterClass CharacterClasses, ulong StringBitMask, byte StringBitMaskBytes, AsciiMap FirstCharMap, AsciiMap LastCharMap);