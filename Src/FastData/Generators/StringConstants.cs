using Genbox.FastData.Generators.Enums;

namespace Genbox.FastData.Generators;

/// <summary>Represents string constants used by code generators in the FastData library.</summary>
public sealed class StringConstants
{
    /// <summary>Gets or sets the minimum string length, if applicable.</summary>
    public required uint MinStringLength { get; init; }

    /// <summary>Gets or sets the maximum string length, if applicable.</summary>
    public required uint MaxStringLength { get; init; }

    /// <summary>If the value type is a string, this will contain the type of characters that exist in the dataset.</summary>
    public required CharacterClass CharacterClasses { get; init; }
}