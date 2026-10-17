using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts.Misc;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for compact hash-table data structures.</summary>
/// <param name="bucketStarts">The array of bucket start indices. The final element is the sentinel end index.</param>
/// <param name="entries">The array of hash-table entries.</param>
/// <param name="storeHashCode"><see langword="true" /> to store each entry's hash code so lookups compare it before the key; <see langword="false" /> to compare keys only.</param>
/// <param name="values">The values associated with the keys.</param>
public sealed class HashTableCompactContext<TKey, TValue>(int[] bucketStarts, HashTableCompactEntry<TKey>[] entries, bool storeHashCode, ReadOnlyMemory<TValue> values) : HashTableCompactContext(bucketStarts, entries.LongLength, storeHashCode)
{
    /// <summary>Gets the array of hash-table entries.</summary>
    public HashTableCompactEntry<TKey>[] Entries { get; } = entries;

    /// <summary>Gets the values emitted into the generated structure.</summary>
    public ReadOnlyMemory<TValue> Values { get; } = values;
}

/// <summary>Provides compact bucket data shared by compact hash-table generated structures.</summary>
public abstract class HashTableCompactContext(int[] bucketStarts, long entryCount, bool storeHashCode) : IContext
{
    /// <summary>Gets the array of bucket start indices. The final element is the sentinel end index.</summary>
    public int[] BucketStarts { get; } = bucketStarts;

    /// <summary>Indicates whether entries store their hash codes so lookups compare them before the key.</summary>
    public bool StoreHashCode { get; } = storeHashCode;

    /// <inheritdoc />
    public virtual long GetOverheadBytes() => (BucketStarts.LongLength * sizeof(int)) + (StoreHashCode ? entryCount * sizeof(ulong) : 0);
}