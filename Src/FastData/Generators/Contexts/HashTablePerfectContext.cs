using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for perfect hash-table data structures.</summary>
/// <param name="data">The array of key-value pairs and their hash codes.</param>
/// <param name="storeHashCode"><see langword="true" /> to store each entry's hash code so lookups compare it before the key; <see langword="false" /> to compare keys only.</param>
/// <param name="values">The values associated with the keys.</param>
public sealed class HashTablePerfectContext<TKey, TValue>(KeyValuePair<TKey, ulong>[] data, bool storeHashCode, ReadOnlyMemory<TValue> values) : HashTablePerfectContext(data.LongLength, storeHashCode)
{
    /// <summary>Gets the array of items and their hash codes.</summary>
    public KeyValuePair<TKey, ulong>[] Data { get; } = data;

    /// <summary>Gets the values emitted into the generated structure.</summary>
    public ReadOnlyMemory<TValue> Values { get; } = values;
}

/// <summary>Provides metadata shared by perfect-hash generated structures.</summary>
public abstract class HashTablePerfectContext(long entryCount, bool storeHashCode) : IContext
{
    /// <summary>Indicates whether entries store their hash codes so lookups compare them before the key.</summary>
    public bool StoreHashCode { get; } = storeHashCode;

    /// <inheritdoc />
    public virtual long GetOverheadBytes() => StoreHashCode ? entryCount * sizeof(ulong) : 0;
}