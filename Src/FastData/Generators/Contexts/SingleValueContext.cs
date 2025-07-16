using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for a single value.</summary>
/// <param name="item">The value to use in the context.</param>
public sealed class SingleValueContext<TKey, TValue>(TKey item, TValue[]? values) : IContext<TValue>
{
    /// <summary>Gets the value used in the context.</summary>
    public TKey Item { get; } = item;
    public TValue[]? Values { get; } = values;
}