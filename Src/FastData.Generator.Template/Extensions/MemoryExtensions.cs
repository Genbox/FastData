using Genbox.FastData.Generator.Template.Misc;

namespace Genbox.FastData.Generator.Template.Extensions;

/// <summary>Provides template-oriented conversions for read-only memory.</summary>
public static class MemoryExtensions
{
    /// <summary>Exposes the elements of a memory region as boxed objects without copying them.</summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="keys">The memory region to enumerate.</param>
    /// <returns>An enumerable view that boxes each element as it is read.</returns>
    public static MemoryObjectEnumerable<T> ToObjects<T>(this ReadOnlyMemory<T> keys) => new MemoryObjectEnumerable<T>(keys);
}