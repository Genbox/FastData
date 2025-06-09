using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for perfect hash set-based data structures.</summary>
/// <typeparam name="T">The type of keys in the key-value pairs.</typeparam>
/// <param name="data">The array of key-value pairs and their hash codes.</param>
public sealed class HashSetPerfectContext<T>(KeyValuePair<T, ulong>[] data) : IContext<T>
{
    /// <summary>Gets the array of items and their hash codes.</summary>
    public KeyValuePair<T, ulong>[] Data { get; } = data;
}