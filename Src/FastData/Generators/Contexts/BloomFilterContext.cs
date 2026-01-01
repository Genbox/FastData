using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for bloom filter-based data structures.</summary>
[SuppressMessage("Major Code Smell", "S2326:Unused type parameters should be removed")]
public sealed class BloomFilterContext<TKey, TValue>(ulong[] bitSet) : IContext<TValue>
{
    public ulong[] BitSet { get; } = bitSet;
    public ReadOnlyMemory<TValue> Values { get; } = ReadOnlyMemory<TValue>.Empty;
}