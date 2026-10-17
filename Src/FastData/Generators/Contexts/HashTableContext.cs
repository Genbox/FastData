using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts.Misc;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for chained hash-table data structures.</summary>
/// <param name="buckets">The array of bucket indices.</param>
/// <param name="entries">The array of hash-table entries.</param>
/// <param name="storeHashCode"><see langword="true" /> to store each entry's hash code so lookups compare it before the key; <see langword="false" /> to compare keys only.</param>
/// <param name="values">The values associated with the keys.</param>
public sealed class HashTableContext<TKey, TValue>(int[] buckets, HashTableEntry<TKey>[] entries, bool storeHashCode, ReadOnlyMemory<TValue> values) : HashTableContext(buckets, entries.LongLength, storeHashCode)
{
    /// <summary>Gets the array of hash-table entries.</summary>
    public HashTableEntry<TKey>[] Entries { get; } = entries;

    /// <summary>Gets the values emitted into the generated structure.</summary>
    public ReadOnlyMemory<TValue> Values { get; } = values;
}

/// <summary>Provides bucket data shared by hash-table generated structures.</summary>
public abstract class HashTableContext(int[] buckets, long entryCount, bool storeHashCode) : IContext
{
    /// <summary>Gets the array of bucket indices.</summary>
    public int[] Buckets { get; } = buckets;

    /// <summary>Indicates whether entries store their hash codes so lookups compare them before the key.</summary>
    public bool StoreHashCode { get; } = storeHashCode;

    /// <inheritdoc />
    public virtual long GetOverheadBytes() => (Buckets.LongLength * sizeof(int)) + (entryCount * sizeof(int)) + (StoreHashCode ? entryCount * sizeof(ulong) : 0);
}