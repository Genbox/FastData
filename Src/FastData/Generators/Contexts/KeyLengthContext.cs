using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for key length-based data structures.</summary>
/// <param name="lengths">An array of lists containing string lengths.</param>
/// <param name="minLength">The minimum string length.</param>
public sealed class KeyLengthContext<TValue>(string?[] lengths, uint minLength, ReadOnlyMemory<TValue> values, int[] valueOffsets) : IContext<TValue>
{
    /// <summary>Gets the array of lists containing string lengths.</summary>
    public string?[] Lengths { get; } = lengths;

    /// <summary>Gets the minimum string length.</summary>
    public uint MinLength { get; } = minLength;

    public ReadOnlyMemory<TValue> Values { get; } = values;
    public int[] ValueOffsets { get; } = valueOffsets;
}