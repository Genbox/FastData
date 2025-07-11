namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for conditional-based (if/switch) data structures.</summary>
public sealed class ConditionalContext<TKey, TValue>(TKey[] keys, TValue[]? values) : DefaultContext<TKey, TValue>(keys, values);