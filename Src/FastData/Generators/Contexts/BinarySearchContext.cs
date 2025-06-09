using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for binary search-based data structures.</summary>
/// <typeparam name="T">The type of elements in the array.</typeparam>
public sealed class BinarySearchContext<T>(T[] data) : IContext<T>
{
    public T[] Data { get; } = data;
}