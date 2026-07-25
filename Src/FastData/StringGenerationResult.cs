using Genbox.FastData.Enums;

namespace Genbox.FastData;

public readonly record struct StringGenerationResult(string Source, string[] EarlyExits, string? StringHashName, StructureType StructureType);