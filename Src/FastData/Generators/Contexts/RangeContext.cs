using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for ranged-based data structures.</summary>
public sealed class RangeContext<TKey>(ReadOnlyMemory<(TKey Start, TKey End)> ranges) : IContext
{
    public ReadOnlyMemory<(TKey Start, TKey End)> Ranges { get; } = ranges;
}