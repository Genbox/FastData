using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for conditional-based (if/switch) data structures.</summary>
/// <typeparam name="T">The type of elements in the array.</typeparam>
public sealed class ConditionalContext<T> : IContext<T>;