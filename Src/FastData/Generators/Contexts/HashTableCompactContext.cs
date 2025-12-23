using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts.Misc;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for compact hash table-based data structures.</summary>
/// <param name="bucketStarts">The array of bucket start indices.</param>
/// <param name="bucketCounts">The array of bucket item counts.</param>
/// <param name="entries">The array of hash table entries.</param>
/// <param name="storeHashCode">If set to true, you should only generate a hash set that checks the value.</param>
public sealed class HashTableCompactContext<TKey, TValue>(int[] bucketStarts, int[] bucketCounts, HashTableCompactEntry<TKey>[] entries, bool storeHashCode, ReadOnlyMemory<TValue> values) : IContext<TValue>
{
    /// <summary>Gets the array of bucket start indices.</summary>
    public int[] BucketStarts { get; } = bucketStarts;

    /// <summary>Gets the array of bucket item counts.</summary>
    public int[] BucketCounts { get; } = bucketCounts;

    /// <summary>Gets the array of hash table entries.</summary>
    public HashTableCompactEntry<TKey>[] Entries { get; } = entries;

    /// <summary>Indicates whether the hash table should store the hash code or only the value.</summary>
    public bool StoreHashCode { get; } = storeHashCode;

    public ReadOnlyMemory<TValue> Values { get; } = values;
}
