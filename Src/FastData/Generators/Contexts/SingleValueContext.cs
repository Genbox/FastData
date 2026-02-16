using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for a single value.</summary>
public class SingleValueContext<TKey, TValue>(TKey key, ReadOnlyMemory<TValue> values) : IContext
{
    public TKey Key { get; } = key;
    public ReadOnlyMemory<TValue> Values { get; } = values;
}