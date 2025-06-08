using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for key length-based data structures.</summary>
/// <param name="lengths">An array of lists containing string lengths.</param>
/// <param name="lengthsAreUniq">Indicates whether all lengths are unique.</param>
/// <param name="minLength">The minimum string length.</param>
/// <param name="maxLength">The maximum string length.</param>
public sealed class KeyLengthContext(List<string>?[] lengths, bool lengthsAreUniq, uint minLength, uint maxLength) : IContext
{
    /// <summary>Gets the array of lists containing string lengths.</summary>
    public List<string>?[] Lengths { get; } = lengths;

    /// <summary>Gets a value indicating whether all lengths are unique.</summary>
    public bool LengthsAreUniq { get; } = lengthsAreUniq;

    /// <summary>Gets the minimum string length.</summary>
    public uint MinLength { get; } = minLength;

    /// <summary>Gets the maximum string length.</summary>
    public uint MaxLength { get; } = maxLength;
}