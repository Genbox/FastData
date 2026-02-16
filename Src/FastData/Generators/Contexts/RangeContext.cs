using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for ranged-based data structures.</summary>
public class RangeContext<TKey>(TKey min, TKey max) : IContext
{
    public TKey Min { get; } = min;
    public TKey Max { get; } = max;
}