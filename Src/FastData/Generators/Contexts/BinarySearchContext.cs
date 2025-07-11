namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for binary search-based data structures.</summary>
/// <typeparam name="T">The type of elements in the array.</typeparam>
public sealed class BinarySearchContext<T>(T[] data) : DefaultContext<T>(data);