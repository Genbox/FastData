using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides the bitset used by Bloom-filter generated structures.</summary>
public sealed class BloomFilterContext(ulong[] bitSet) : IContext
{
    /// <summary>Gets the Bloom filter bitset.</summary>
    public ulong[] BitSet { get; } = bitSet;
}