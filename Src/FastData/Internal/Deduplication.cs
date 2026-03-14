using Genbox.FastData.Config;

namespace Genbox.FastData.Internal;

internal static class Deduplication
{
    internal static bool DeduplicateKeys<TKey, TValue>(DataConfig fdCfg, ref ReadOnlyMemory<TKey> keys, ref ReadOnlyMemory<TValue> values, IEqualityComparer<TKey> equalityComparer, IComparer<TKey> sortComparer)
    {
        // Apply the configured strategy and return new key/value buffers.
        if (fdCfg.DeduplicationMode == DeduplicationMode.Disabled)
            return false;

        TKey[] copyKeys = new TKey[keys.Length];
        keys.CopyTo(copyKeys);

        TValue[] copyValues = new TValue[values.Length];
        values.CopyTo(copyValues);

        bool isSorted = false;
        int uniqueCount;

        if (fdCfg.DeduplicationMode == DeduplicationMode.HashSet)
            DeduplicateWithHashSet(copyKeys, copyValues, fdCfg.ThrowOnDuplicates, equalityComparer, out uniqueCount);
        else if (fdCfg.DeduplicationMode == DeduplicationMode.Sort && fdCfg.PreserveOrder)
            DeduplicateWithSortPreserveInputOrder(copyKeys, copyValues, fdCfg.ThrowOnDuplicates, equalityComparer, sortComparer, out uniqueCount);
        else if (fdCfg.DeduplicationMode == DeduplicationMode.Sort)
        {
            DeduplicateWithSort(copyKeys, copyValues, fdCfg.ThrowOnDuplicates, equalityComparer, sortComparer, out uniqueCount);
            isSorted = true;
        }
        else
            throw new InvalidOperationException("Unsupported deduplication mode: " + fdCfg.DeduplicationMode);

        keys = copyKeys.AsMemory(0, uniqueCount);
        values = copyValues.Length > 0 ? copyValues.AsMemory(0, uniqueCount) : copyValues;
        return isSorted;
    }

    internal static void DeduplicateWithHashSet<TKey, TValue>(TKey[] keys, TValue[] values, bool throwEnabled, IEqualityComparer<TKey> equalityComparer, out int uniqueCount)
    {
        HashSet<TKey> uniq = new HashSet<TKey>(equalityComparer);

        uniqueCount = 0;

        for (int i = 0; i < keys.Length; i++)
        {
            TKey key = keys[i];

            if (!uniq.Add(key))
            {
                if (throwEnabled)
                    throw new InvalidOperationException($"Duplicate key found: {key}");

                continue;
            }

            keys[uniqueCount] = key;

            if (values.Length != 0 && uniqueCount != i) // Check avoids swapping an element with itself
                values[uniqueCount] = values[i];

            uniqueCount++;
        }
    }

    internal static void DeduplicateWithSortPreserveInputOrder<TKey, TValue>(TKey[] keys, TValue[] values, bool throwEnabled, IEqualityComparer<TKey> equalityComparer, IComparer<TKey> sortComparer, out int uniqueCount)
    {
        // Create a map to keep track of the original order. We need it to map values back to the original order.
        // We also need it to map values (if any).
        int[] map = new int[keys.Length];

        for (int i = 0; i < map.Length; i++)
            map[i] = i;

        /*
             = Starting state =
             keys  values  map
              6     val6    1
              9     val9    2
              3     val3    3
              1     val1    4
              3     val3    5
        */

        Array.Sort(keys, map, sortComparer);

        /*
             = After sorting =
             keys  values  map
              1     val6    4
              3     val9    3
              3     val3    5
              6     val1    1
              9     val3    2
        */

        uniqueCount = 0;
        for (int read = 1; read < keys.Length; read++)
        {
            TKey key = keys[read];

            if (equalityComparer.Equals(key, keys[uniqueCount]))
            {
                if (throwEnabled)
                    throw new InvalidOperationException($"Duplicate key found: {key}");

                continue;
            }

            uniqueCount++;
            keys[uniqueCount] = key;
            map[uniqueCount] = map[read];
        }

        uniqueCount++; // It is off-by-one now. We correct it

        // Sort keys back to original order
        Array.Sort(map, keys, 0, uniqueCount);

        // If we have values, make sure they are compacted too
        if (values.Length != 0)
        {
            for (int i = 0; i < uniqueCount; i++)
                values[i] = values[map[i]];
        }
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