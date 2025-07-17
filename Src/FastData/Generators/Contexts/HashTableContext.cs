using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts.Misc;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for hash set chain-based data structure.</summary>
/// <param name="buckets">The array of bucket indices.</param>
/// <param name="entries">The array of hash set entries.</param>
/// <param name="storeHashCode">If set to true, you should only generate a hash set that checks the value.</param>
public sealed class HashTableContext<TKey, TValue>(int[] buckets, HashTableEntry<TKey>[] entries, bool storeHashCode, TValue[]? values) : IContext<TValue>
{
    /// <summary>Gets the array of bucket indices.</summary>
    public int[] Buckets { get; } = buckets;

    /// <summary>Gets the array of hash set entries.</summary>
    public HashTableEntry<TKey>[] Entries { get; } = entries;

    /// <summary>Indicates whether the hash set should store the hash code or only the value.</summary>
    public bool StoreHashCode { get; } = storeHashCode;
    public TValue[]? Values { get; } = values;
}