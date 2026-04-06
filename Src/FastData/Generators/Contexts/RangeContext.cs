using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for ranged-based data structures.</summary>
public sealed class RangeContext<TKey> : IContext
{
    public TKey Min { get; } = min;
    public TKey Max { get; } = max;
}