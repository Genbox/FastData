namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for array-based data structures.</summary>
public sealed class ArrayContext<TKey, TValue>(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values) : DefaultContext<TKey, TValue>(keys, values);