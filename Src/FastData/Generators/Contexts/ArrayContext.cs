namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for array-based data structures.</summary>
public sealed class ArrayContext<TKey, TValue>(TKey[] keys, ValueSpec<TValue>? valueSpec) : DefaultContext<TKey, TValue>(keys, valueSpec);