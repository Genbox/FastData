using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for a single value.</summary>
public sealed class SingleValueContext<TKey, TValue>(TKey key, ReadOnlyMemory<TValue> values) : IContext
{
    /// <summary>Gets the single stored key.</summary>
    public TKey Key { get; } = key;

    /// <summary>Gets the value associated with the key, when present.</summary>
    public ReadOnlyMemory<TValue> Values { get; } = values;

    /// <inheritdoc />
    public long GetOverheadBytes() => 0;
}