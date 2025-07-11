using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for perfect hash set-based data structures.</summary>
/// <param name="data">The array of key-value pairs and their hash codes.</param>
/// <param name="storeHashCode">If set to true, you should only generate a hash set that checks the value.</param>
public sealed class HashTablePerfectContext<TKey, TValue>(KeyValuePair<TKey, ulong>[] data, bool storeHashCode, TValue[]? values) : IContext
{
    /// <summary>Gets the array of items and their hash codes.</summary>
    public KeyValuePair<TKey, ulong>[] Data { get; } = data;

    /// <summary>Indicates whether the hash set should store the hash code or only the value.</summary>
    public bool StoreHashCode { get; } = storeHashCode;
    public TValue[]? Values { get; } = values;
}