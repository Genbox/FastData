namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for binary search-based data structures.</summary>
public sealed class BinarySearchContext<TKey, TValue>(TKey[] keys, TValue[]? values) : DefaultContext<TKey, TValue>(keys, values);