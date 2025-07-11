namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for Eytzinger layout search-based data structures.</summary>
/// <typeparam name="T">The type of elements in the array.</typeparam>
public sealed class EytzingerSearchContext<T>(T[] data) : DefaultContext<T>(data);