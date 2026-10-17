using Genbox.FastData.Generators.Enums;

namespace Genbox.FastData.Generators;

/// <summary>Represents string constants used by code generators in the FastData library.</summary>
public sealed class StringConstants
{
    /// <summary>Gets the length of the shortest key.</summary>
    public required int MinStringLength { get; init; }

    /// <summary>Gets the length of the longest key.</summary>
    public required int MaxStringLength { get; init; }

    /// <summary>Gets the character classes present in the keys.</summary>
    public required CharacterClass CharacterClasses { get; init; }
}