using Genbox.FastData.Config;

namespace Genbox.FastData.Internal;

internal static class Deduplication
{
    internal static void DeduplicateKeys<TKey, TValue>(DataConfig fdCfg, ref ReadOnlyMemory<TKey> keys, ref ReadOnlyMemory<TValue> values, IEqualityComparer<TKey> equalityComparer, IComparer<TKey> sortComparer)
    {
        TKey[] copyKeys = new TKey[keys.Length];
        keys.CopyTo(copyKeys);

        TValue[] copyValues = new TValue[values.Length];
        values.CopyTo(copyValues);

        DeduplicateWithSort(copyKeys, copyValues, fdCfg.ThrowOnDuplicates, equalityComparer, sortComparer, out int uniqueCount);

        keys = copyKeys.AsMemory(0, uniqueCount);
        values = copyValues.Length > 0 ? copyValues.AsMemory(0, uniqueCount) : copyValues;
    }

    internal static void DeduplicateWithSort<TKey, TValue>(TKey[] keys, TValue[] values, bool throwEnabled, IEqualityComparer<TKey> equalityComparer, IComparer<TKey> sortComparer, out int uniqueCount)
    {
        if (values.Length > 0)
            Array.Sort(keys, values, sortComparer);
        else
            Array.Sort(keys, sortComparer);

        TKey current = keys[0];
        uniqueCount = 1;

        for (int i = 1; i < keys.Length; i++)
        {
            TKey key = keys[i];

            if (equalityComparer.Equals(key, current))
            {
                if (throwEnabled)
                    throw new InvalidOperationException($"Duplicate key found: {key}");

                continue;
            }

            keys[uniqueCount] = key;

            if (values.Length != 0 && uniqueCount != i) // Check avoids swapping an element with itself
                values[uniqueCount] = values[i];

            current = key;
            uniqueCount++;
        }
    }
}