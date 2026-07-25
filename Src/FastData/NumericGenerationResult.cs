using Genbox.FastData.Enums;

namespace Genbox.FastData;

public readonly record struct NumericGenerationResult(string Source, string[] EarlyExits, StructureType StructureType);