namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for binary search-based data structures.</summary>
public sealed class BinarySearchContext<TKey, TValue>(TKey[] keys, ValueSpec<TValue>? valueSpec) : DefaultContext<TKey, TValue>(keys, valueSpec);