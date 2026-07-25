using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides bitset and value data for bitset generated structures.</summary>
public sealed class BitSetContext<TValue>(ulong[] bitSet, ReadOnlyMemory<TValue> values, bool hasOccupancy = true) : BitSetContext(bitSet, hasOccupancy)
{
    /// <summary>Gets the values emitted into the generated structure.</summary>
    public ReadOnlyMemory<TValue> Values { get; } = values;
}

/// <summary>Provides bitset data for bitset generated structures.</summary>
public abstract class BitSetContext(ulong[] bitSet, bool hasOccupancy = true) : IContext
{
    /// <summary>Gets the membership bitset.</summary>
    public ulong[] BitSet { get; } = bitSet;

    /// <summary>Indicates whether generated lookup must test an occupancy bit before indexing values.</summary>
    public bool HasOccupancy { get; } = hasOccupancy;

    /// <inheritdoc />
    public long GetOverheadBytes() => BitSet.LongLength * sizeof(ulong);
}