using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for key length-based data structures.</summary>
/// <param name="lengths">The keys indexed by length minus <paramref name="minLength" />, with <see langword="null" /> where no key has that length.</param>
/// <param name="minLength">The length of the shortest key.</param>
/// <param name="values">The values associated with the keys.</param>
/// <param name="valueOffsets">The value index associated with each length slot.</param>
public sealed class KeyLengthContext<TValue>(string?[] lengths, int minLength, ReadOnlyMemory<TValue> values, int[] valueOffsets) : KeyLengthContext(lengths, minLength, valueOffsets)
{
    /// <summary>Gets the values emitted into the generated structure.</summary>
    public ReadOnlyMemory<TValue> Values { get; } = values;
}

/// <summary>Provides length-indexed key data for key-length generated structures.</summary>
public abstract class KeyLengthContext(string?[] lengths, int minLength, int[] valueOffsets) : IContext
{
    /// <summary>Gets the keys indexed by length minus <see cref="MinLength" />, with <see langword="null" /> where no key has that length.</summary>
    public string?[] Lengths { get; } = lengths;

    /// <summary>Gets the length of the shortest key.</summary>
    public int MinLength { get; } = minLength;

    /// <summary>Gets the value index associated with each length slot.</summary>
    public int[] ValueOffsets { get; } = valueOffsets;

    /// <inheritdoc />
    public virtual long GetOverheadBytes() => ValueOffsets.LongLength * sizeof(int);
}