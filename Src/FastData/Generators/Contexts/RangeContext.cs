using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for ranged-based data structures.</summary>
public sealed class RangeContext<TKey, TValue>(TKey min, TKey max) : IContext<TValue>
{
    public TKey Min { get; } = min;
    public TKey Max { get; } = max;

    public ReadOnlyMemory<TValue> Values { get; } = ReadOnlyMemory<TValue>.Empty; // Range does not support values
}