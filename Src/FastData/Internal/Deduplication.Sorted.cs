using System.Runtime.CompilerServices;

namespace Genbox.FastData.Internal;

internal static partial class Deduplication
{
    private static void CompactSorted<TKey, TValue>(Span<TKey> keys, Span<TValue> values, out int uniqueCount)
    {
        TKey current = keys[0];
        uniqueCount = 1;

        for (int i = 1; i < keys.Length; i++)
        {
            TKey key = keys[i];

            if (EqualityComparer<TKey>.Default.Equals(key, current))
                continue;

            keys[uniqueCount] = key;

            if (values.Length != 0 && uniqueCount != i)
                values[uniqueCount] = values[i];

            current = key;
            uniqueCount++;
        }
    }

    private static void CompactSorted<TKey, TValue>(TKey[] keys, TValue[] values, IEqualityComparer<TKey> equalityComparer, out int uniqueCount)
    {
        TKey current = keys[0];
        uniqueCount = 1;

        for (int i = 1; i < keys.Length; i++)
        {
            TKey key = keys[i];

            if (equalityComparer.Equals(key, current))
                continue;

            keys[uniqueCount] = key;

            if (values.Length != 0 && uniqueCount != i)
                values[uniqueCount] = values[i];

            current = key;
            uniqueCount++;
        }
    }

    private static bool TryCompactSortedInput<TKey, TValue>(TKey[] keys, TValue[] values, out int uniqueCount)
    {
        // Some callers already provide sorted data. Detect that while compacting and exit on disorder.
        if (values.Length == 0)
            return TryCompactSortedComparableKeys(keys, out uniqueCount);

        Comparer<TKey> orderComparer = Comparer<TKey>.Default;
        EqualityComparer<TKey> equalityComparer = EqualityComparer<TKey>.Default;

        TKey lastOrdered = keys[0];
        TKey lastUnique = keys[0];
        uniqueCount = 1;

        ref TKey keysRef = ref keys[0];
        ref TValue valuesRef = ref values[0];

        for (int i = 1; i < keys.Length; i++)
        {
            TKey key = Unsafe.Add(ref keysRef, i);

            if (orderComparer.Compare(lastOrdered, key) > 0)
            {
                uniqueCount = 0;
                return false;
            }

            lastOrdered = key;

            if (equalityComparer.Equals(key, lastUnique))
                continue;

            Unsafe.Add(ref keysRef, uniqueCount) = key;

            if (uniqueCount != i)
                Unsafe.Add(ref valuesRef, uniqueCount) = Unsafe.Add(ref valuesRef, i);

            lastUnique = key;
            uniqueCount++;
        }

        return true;

        static bool TryCompactSortedComparableKeys(Span<TKey> keys1, out int uniqueCount1)
        {
            Comparer<TKey> orderComparer = Comparer<TKey>.Default;
            EqualityComparer<TKey> equalityComparer = EqualityComparer<TKey>.Default;

            TKey lastOrdered = keys1[0];
            TKey lastUnique = keys1[0];
            uniqueCount1 = 1;

            ref TKey keysRef = ref keys1[0];

            for (int i = 1; i < keys1.Length; i++)
            {
                TKey key = Unsafe.Add(ref keysRef, i);

                if (orderComparer.Compare(lastOrdered, key) > 0)
                {
                    uniqueCount1 = 0;
                    return false;
                }

                lastOrdered = key;

                if (equalityComparer.Equals(key, lastUnique))
                    continue;

                Unsafe.Add(ref keysRef, uniqueCount1) = key;
                lastUnique = key;
                uniqueCount1++;
            }

            return true;
        }
    }

    private static void SortFallback<TKey, TValue>(TKey[] keys, TValue[] values, IComparer<TKey> comparer)
    {
        // .NET does not always avoid virtual dispatch when the comparer is the default instance.
        // A type check catches both the singleton and structurally-equivalent default comparers.
        if (ReferenceEquals(comparer, Comparer<TKey>.Default) || comparer.GetType() == Comparer<TKey>.Default.GetType())
        {
            if (values.Length > 0)
                Array.Sort(keys, values);
            else
                Array.Sort(keys);

            return;
        }

        if (values.Length > 0)
            Array.Sort(keys, values, comparer);
        else
            Array.Sort(keys, comparer);
    }
}