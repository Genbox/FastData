using Genbox.FastData.Config;

namespace Genbox.FastData.Internal;

internal static partial class Deduplication
{
    /*
     * The common case this code optimizes for is numeric key sets with a small value span. Sorting is O(n log n) and moves the value array with the keys,
     * while a bounded seen-map pass is O(n + range). The tradeoff is that we must first prove the numeric range is small and dense enough to keep the temporary map
     * bounded, then fall back to sorting for unsupported types, custom comparers, and sparse or wide ranges.
     */

    internal static void DeduplicateStringKeys<TValue>(DataConfig fdCfg, ref ReadOnlyMemory<string> keys, ref ReadOnlyMemory<TValue> values, IEqualityComparer<string> equalityComparer, IComparer<string> sortComparer)
    {
        // Upstream callers validate keys and values before deduplication.

        string[] keyCopy = new string[keys.Length];
        keys.CopyTo(keyCopy);

        TValue[] valCopy = new TValue[values.Length];
        values.CopyTo(valCopy);

        DeduplicateStringKeysInternal(keyCopy, valCopy, equalityComparer, sortComparer, out int uniqueCount);

        if (fdCfg.ThrowOnDuplicates && uniqueCount != keys.Length)
            throw new InvalidOperationException("Duplicate key found.");

        if (ShouldShrink(keyCopy.Length, uniqueCount))
        {
            keyCopy = CopySlice(keyCopy, uniqueCount);

            if (valCopy.Length != 0)
                valCopy = CopySlice(valCopy, uniqueCount);
        }

        keys = keyCopy.AsMemory(0, uniqueCount);
        values = valCopy.Length > 0 ? valCopy.AsMemory(0, uniqueCount) : valCopy;
    }

    internal static void DeduplicateStringKeysInternal<TValue>(string[] keys, TValue[] values, IEqualityComparer<string> equalityComparer, IComparer<string> sortComparer, out int uniqueCount)
    {
        SortFallback(keys, values, sortComparer);
        CompactSorted(keys, values, equalityComparer, out uniqueCount);
    }

    internal static void DeduplicateNumericKeys<TKey, TValue>(DataConfig fdCfg, ref ReadOnlyMemory<TKey> keys, ref ReadOnlyMemory<TValue> values)
    {
        // Upstream callers validate keys and values before deduplication.

        // For finite domains smaller than the input, a duplicate is guaranteed. This lets ThrowOnDuplicates fail without building any temporary maps.
        if (fdCfg.ThrowOnDuplicates && HasPigeonholeDuplicate<TKey>(keys.Length))
            throw new InvalidOperationException("Duplicate key found.");

        TKey[] keyCopy = new TKey[keys.Length];
        keys.CopyTo(keyCopy);

        TValue[] valCopy = new TValue[values.Length];
        values.CopyTo(valCopy);

        DeduplicateNumericKeysInternal(keyCopy, valCopy, out int uniqueCount);

        if (fdCfg.ThrowOnDuplicates && uniqueCount != keys.Length)
            throw new InvalidOperationException("Duplicate key found.");

        if (ShouldShrink(keyCopy.Length, uniqueCount))
        {
            keyCopy = CopySlice(keyCopy, uniqueCount);

            if (valCopy.Length != 0)
                valCopy = CopySlice(valCopy, uniqueCount);
        }

        keys = keyCopy.AsMemory(0, uniqueCount);
        values = valCopy.Length > 0 ? valCopy.AsMemory(0, uniqueCount) : valCopy;
    }

    internal static void DeduplicateNumericKeysInternal<TKey, TValue>(TKey[] keys, TValue[] values, out int uniqueCount)
    {
        // Prefer already-sorted input first: it avoids both Array.Sort and mapped-range setup.
        if (TryCompactSortedInput(keys, values, out uniqueCount))
            return;

        if (ShouldUseBitSet(keys.Length))
        {
            if (TryDeduplicateWithBitSet(keys, values, out uniqueCount))
                return;
        }
        else
        {
            if (TryDeduplicateWithRange(keys, values, out uniqueCount))
                return;
        }

        SortFallback(keys, values, Comparer<TKey>.Default);
        CompactSorted(keys, values, out uniqueCount);
    }

    private static bool TryDeduplicateWithRange<TKey, TValue>(TKey[] keys, TValue[] values, out int uniqueCount)
    {
        uniqueCount = 0;

        // Generic numeric math is not available for netstandard2.0, so use type dispatch to keep each hot loop specialized and allocation-aware.
        if (typeof(TKey) == typeof(byte))
        {
            uniqueCount = DeduplicateUInt8((byte[])(object)keys, values);
            return true;
        }

        if (typeof(TKey) == typeof(sbyte))
        {
            uniqueCount = DeduplicateInt8((sbyte[])(object)keys, values);
            return true;
        }

        if (typeof(TKey) == typeof(char))
            return TryDeduplicateChar((char[])(object)keys, values, out uniqueCount);

        if (typeof(TKey) == typeof(short))
            return TryDeduplicateInt16((short[])(object)keys, values, out uniqueCount);

        if (typeof(TKey) == typeof(ushort))
            return TryDeduplicateUInt16((ushort[])(object)keys, values, out uniqueCount);

        if (typeof(TKey) == typeof(int))
            return TryDeduplicateInt32((int[])(object)keys, values, out uniqueCount);

        if (typeof(TKey) == typeof(uint))
            return TryDeduplicateUInt32((uint[])(object)keys, values, out uniqueCount);

        if (typeof(TKey) == typeof(long))
            return TryDeduplicateInt64((long[])(object)keys, values, out uniqueCount);

        if (typeof(TKey) == typeof(ulong))
            return TryDeduplicateUInt64((ulong[])(object)keys, values, out uniqueCount);

        return false;
    }

    private static bool TryDeduplicateWithBitSet<TKey, TValue>(TKey[] keys, TValue[] values, out int uniqueCount)
    {
        uniqueCount = 0;

        if (typeof(TKey) == typeof(int))
            return TryDeduplicateInt32BitSet((int[])(object)keys, values, out uniqueCount);

        if (typeof(TKey) == typeof(uint))
            return TryDeduplicateUInt32BitSet((uint[])(object)keys, values, out uniqueCount);

        if (typeof(TKey) == typeof(long))
            return TryDeduplicateInt64BitSet((long[])(object)keys, values, out uniqueCount);

        if (typeof(TKey) == typeof(ulong))
            return TryDeduplicateUInt64BitSet((ulong[])(object)keys, values, out uniqueCount);

        return false;
    }
}