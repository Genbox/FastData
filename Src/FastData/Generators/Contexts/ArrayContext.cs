using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides key and value data for array-based generated structures.</summary>
public sealed class ArrayContext<TKey, TValue>(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values) : IContext
{
    /// <summary>Gets the keys emitted into the generated structure.</summary>
    public ReadOnlyMemory<TKey> Keys { get; } = keys;

    /// <summary>Gets the values emitted into the generated structure.</summary>
    public ReadOnlyMemory<TValue> Values { get; } = values;
}