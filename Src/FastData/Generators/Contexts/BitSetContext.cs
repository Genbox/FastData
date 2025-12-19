using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for bitset-based data structures.</summary>
[SuppressMessage("Major Code Smell", "S2326:Unused type parameters should be removed")]
public sealed class BitSetContext<TKey, TValue>(ulong[] bitSet, TValue[]? values) : IContext<TValue>
{
    public ulong[] BitSet { get; } = bitSet;
    public TValue[]? Values { get; } = values;
}
