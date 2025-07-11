namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for Eytzinger layout search-based data structures.</summary>
public sealed class EytzingerSearchContext<TKey, TValue>(TKey[] keys, TValue[]? values) : DefaultContext<TKey, TValue>(keys, values);