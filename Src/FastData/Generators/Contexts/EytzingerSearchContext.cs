namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for Eytzinger layout search-based data structures.</summary>
public sealed class EytzingerSearchContext<TKey, TValue>(TKey[] keys, ValueSpec<TValue>? valueSpec) : DefaultContext<TKey, TValue>(keys, valueSpec);