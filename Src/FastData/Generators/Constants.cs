namespace Genbox.FastData.Generators;

/// <summary>Represents a set of constants used by code generators in the FastData library.</summary>
/// <typeparam name="TKey">The type of value for min/max value constants.</typeparam>
public sealed class Constants<TKey>(uint itemCount)
{
    /// <summary>Gets the number of items for which the constants are defined.</summary>
    public uint ItemCount { get; } = itemCount;

    /// <summary>Gets or sets the minimum value for the data type.</summary>
    public TKey MinValue { get; set; } = default!;

    /// <summary>Gets or sets the maximum value for the data type.</summary>
    public TKey MaxValue { get; set; } = default!;

    /// <summary>Gets or sets the minimum string length, if applicable.</summary>
    public uint MinStringLength { get; set; }

    /// <summary>Gets or sets the maximum string length, if applicable.</summary>
    public uint MaxStringLength { get; set; }

    /// <summary>If the value type is a string, this will contain the type of characters that exist in the dataset.</summary>
    public CharacterClass CharacterClasses { get; set; }
}