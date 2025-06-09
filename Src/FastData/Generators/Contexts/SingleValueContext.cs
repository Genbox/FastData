using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for a single value.</summary>
/// <typeparam name="T">The type of the value.</typeparam>
/// <param name="item">The value to use in the context.</param>
public sealed class SingleValueContext<T>(T item) : IContext<T>
{
    /// <summary>Gets the value used in the context.</summary>
    public T Item { get; } = item;
}