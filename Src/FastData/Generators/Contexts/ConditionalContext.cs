namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for conditional-based (if/switch) data structures.</summary>
public sealed class ConditionalContext<TKey, TValue>(TKey[] keys, ValueSpec<TValue>? valueSpec) : DefaultContext<TKey, TValue>(keys, valueSpec);