using System.Runtime.CompilerServices;
using Genbox.FastData.Config;
using static Genbox.FastData.Internal.ValueHelper;

namespace Genbox.FastData.Internal;

internal static class Deduplication
{
    private const int MaxMappedRange = ushort.MaxValue;
    private const int MaxMappedRangeWithValues = 16 * 1024;
    private const int MaxMappedRangeToKeyCountFactor = 16;
    private const int MappedRangeProbeSampleCount = 64;
    private const int ByteValueCount = byte.MaxValue + 1;
    private const int ShrinkThresholdDivisor = 2;

    /*
     * The common case this code optimizes for is numeric key sets with a small value span. Sorting is O(n log n) and moves the value array with the keys,
     * while a bounded seen-map pass is O(n + range). The tradeoff is that we must first prove the numeric range is small and dense enough to keep the temporary map
     * bounded, then fall back to sorting for unsupported types, custom comparers, and sparse or wide ranges.
     */

    internal static void DeduplicateStringKeys<TValue>(DataConfig fdCfg, ref ReadOnlyMemory<string> keys, ref ReadOnlyMemory<TValue> values, IEqualityComparer<string> equalityComparer, IComparer<string> sortComparer)
    {
        if (values.Length != 0 && values.Length != keys.Length)
            throw new ArgumentException("Values must be empty or have the same length as keys.", nameof(values));

        if (keys.Length <= 1)
            return;

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
        if (values.Length != 0 && values.Length != keys.Length)
            throw new ArgumentException("Values must be empty or have the same length as keys.", nameof(values));

        // Empty and single-item arrays are already unique.
        if (keys.Length <= 1)
            return;

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
        // Prefer already-sorted input first: it avoids both Array.Sort and mapped-range setup, then fall back to numeric maps and finally sorting.
        if (!TryCompactSortedInput(keys, values, out uniqueCount) && !TryDeduplicateWithRange(keys, values, out uniqueCount))
        {
            SortFallback(keys, values, Comparer<TKey>.Default);
            CompactSorted(keys, values, out uniqueCount);
        }
    }

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

        TKey previous = keys[0];
        TKey current = keys[0];
        uniqueCount = 1;

        ref TKey keysRef = ref keys[0];
        ref TValue valuesRef = ref values[0];

        for (int i = 1; i < keys.Length; i++)
        {
            TKey key = Unsafe.Add(ref keysRef, i);

            if (Comparer<TKey>.Default.Compare(previous, key) > 0)
            {
                uniqueCount = 0;
                return false;
            }

            previous = key;

            if (key!.Equals(current))
                continue;

            Unsafe.Add(ref keysRef, uniqueCount) = key;

            if (uniqueCount != i)
                Unsafe.Add(ref valuesRef, uniqueCount) = Unsafe.Add(ref valuesRef, i);

            current = key;
            uniqueCount++;
        }

        return true;

        static bool TryCompactSortedComparableKeys(Span<TKey> keys1, out int uniqueCount1)
        {
            TKey previous = keys1[0];
            TKey current = keys1[0];
            uniqueCount1 = 1;

            ref TKey keysRef = ref keys1[0];

            for (int i = 1; i < keys1.Length; i++)
            {
                TKey key = Unsafe.Add(ref keysRef, i);

                if (Comparer<TKey>.Default.Compare(previous, key) > 0)
                {
                    uniqueCount1 = 0;
                    return false;
                }

                previous = key;

                if (key!.Equals(current))
                    continue;

                Unsafe.Add(ref keysRef, uniqueCount1) = key;
                current = key;
                uniqueCount1++;
            }

            return true;
        }
    }

    private static bool TryDeduplicateWithRange<TKey, TValue>(TKey[] keys, TValue[] values, out int uniqueCount)
    {
        uniqueCount = 0;

        // Generic numeric math is not available for netstandard2.0, so use type dispatch to keep each hot loop specialized and allocation-aware.
        if (typeof(TKey) == typeof(byte))
        {
            uniqueCount = DeduplicateByte((byte[])(object)keys, values);
            return true;
        }

        if (typeof(TKey) == typeof(sbyte))
        {
            uniqueCount = DeduplicateSByte((sbyte[])(object)keys, values);
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

    private static int DeduplicateByte<TValue>(Span<byte> keys, Span<TValue> values)
    {
        // Keep the value-aware loop branch-free for callers that provide values.
        if (values.Length == 0)
            return DeduplicateByteKeys(keys);

        Span<byte> seen = stackalloc byte[ByteValueCount];
        seen.Clear();

        // Map byte keys directly to their first value, then emit keys in byte order.
        Span<TValue> valueMap = new TValue[ByteValueCount];

        ref byte keysRef = ref keys[0];
        ref TValue valuesRef = ref values[0];
        ref byte seenRef = ref seen[0];
        ref TValue valueMapRef = ref valueMap[0];

        for (int i = 0; i < keys.Length; i++)
        {
            byte key = Unsafe.Add(ref keysRef, i);
            ref byte seenValue = ref Unsafe.Add(ref seenRef, key);

            if (seenValue != 0)
                continue;

            seenValue = 1;
            Unsafe.Add(ref valueMapRef, key) = Unsafe.Add(ref valuesRef, i);
        }

        int writeIndex = 0;

        for (int i = 0; i < ByteValueCount; i++)
        {
            if (Unsafe.Add(ref seenRef, i) == 0)
                continue;

            Unsafe.Add(ref keysRef, writeIndex) = (byte)i;
            Unsafe.Add(ref valuesRef, writeIndex) = Unsafe.Add(ref valueMapRef, i);
            writeIndex++;
        }

        return writeIndex;

        static int DeduplicateByteKeys(Span<byte> keys)
        {
            // A byte key can be used directly as an index into the seen map.
            Span<byte> seen = stackalloc byte[ByteValueCount];
            seen.Clear();

            // Cache refs once so the hot loops can use Unsafe.Add instead of indexers.
            ref byte keysRef = ref keys[0];
            ref byte seenRef = ref seen[0];

            for (int i = 0; i < keys.Length; i++)
            {
                byte key = Unsafe.Add(ref keysRef, i);
                ref byte seenValue = ref Unsafe.Add(ref seenRef, key);

                if (seenValue != 0)
                    continue;

                seenValue = 1;
            }

            // Compact by recreating sorted keys from the seen map.
            int writeIndex = 0;
            for (int i = 0; i < ByteValueCount; i++)
            {
                if (Unsafe.Add(ref seenRef, i) == 0)
                    continue;

                Unsafe.Add(ref keysRef, writeIndex) = (byte)i;
                writeIndex++;
            }

            return writeIndex;
        }
    }

    private static int DeduplicateSByte<TValue>(Span<sbyte> keys, Span<TValue> values)
    {
        if (values.Length == 0)
            return DeduplicateSByteKeys(keys);

        Span<byte> seen = stackalloc byte[ByteValueCount];
        seen.Clear();

        // Bias signed bytes into sorted index order: -128..127 maps to 0..255.
        Span<TValue> valueMap = new TValue[ByteValueCount];

        ref sbyte keysRef = ref keys[0];
        ref TValue valuesRef = ref values[0];
        ref byte seenRef = ref seen[0];
        ref TValue valueMapRef = ref valueMap[0];

        for (int i = 0; i < keys.Length; i++)
        {
            sbyte key = Unsafe.Add(ref keysRef, i);
            int index = key - sbyte.MinValue;
            ref byte seenValue = ref Unsafe.Add(ref seenRef, index);

            if (seenValue != 0)
                continue;

            seenValue = 1;
            Unsafe.Add(ref valueMapRef, index) = Unsafe.Add(ref valuesRef, i);
        }

        int writeIndex = 0;

        for (int i = 0; i < ByteValueCount; i++)
        {
            if (Unsafe.Add(ref seenRef, i) == 0)
                continue;

            Unsafe.Add(ref keysRef, writeIndex) = (sbyte)(i + sbyte.MinValue);
            Unsafe.Add(ref valuesRef, writeIndex) = Unsafe.Add(ref valueMapRef, i);
            writeIndex++;
        }

        return writeIndex;

        static int DeduplicateSByteKeys(Span<sbyte> keys)
        {
            Span<byte> seen = stackalloc byte[ByteValueCount];
            seen.Clear();

            ref sbyte keysRef = ref keys[0];
            ref byte seenRef = ref seen[0];

            for (int i = 0; i < keys.Length; i++)
            {
                sbyte key = Unsafe.Add(ref keysRef, i);
                int index = key - sbyte.MinValue;
                ref byte seenValue = ref Unsafe.Add(ref seenRef, index);

                if (seenValue != 0)
                    continue;

                seenValue = 1;
            }

            int writeIndex = 0;

            for (int i = 0; i < ByteValueCount; i++)
            {
                if (Unsafe.Add(ref seenRef, i) == 0)
                    continue;

                Unsafe.Add(ref keysRef, writeIndex) = (sbyte)(i + sbyte.MinValue);
                writeIndex++;
            }

            return writeIndex;
        }
    }

    private static bool TryDeduplicateChar<TValue>(Span<char> keys, Span<TValue> values, out int uniqueCount)
    {
        if (!TryGetCharMappedRange(keys, out char min, out int range) || !IsMappedRangeUsable(keys.Length, values.Length, range))
        {
            uniqueCount = 0;
            return false;
        }

        if (values.Length == 0)
        {
            uniqueCount = DeduplicateChar(keys, min, range);
            return true;
        }

        Span<byte> seen = stackalloc byte[range];
        seen.Clear();
        Span<TValue> valueMap = new TValue[range];

        ref char keysRef = ref keys[0];
        ref TValue valuesRef = ref values[0];
        ref byte seenRef = ref seen[0];
        ref TValue valueMapRef = ref valueMap[0];

        for (int i = 0; i < keys.Length; i++)
        {
            char key = Unsafe.Add(ref keysRef, i);
            int index = key - min;
            ref byte seenValue = ref Unsafe.Add(ref seenRef, index);

            if (seenValue != 0)
                continue;

            seenValue = 1;
            Unsafe.Add(ref valueMapRef, index) = Unsafe.Add(ref valuesRef, i);
        }

        int writeIndex = 0;

        for (int i = 0; i < range; i++)
        {
            if (Unsafe.Add(ref seenRef, i) == 0)
                continue;

            Unsafe.Add(ref keysRef, writeIndex) = (char)(i + min);
            Unsafe.Add(ref valuesRef, writeIndex) = Unsafe.Add(ref valueMapRef, i);
            writeIndex++;
        }

        uniqueCount = writeIndex;
        return true;

        static bool TryGetCharMappedRange(ReadOnlySpan<char> keys, out char min, out int range)
        {
            // Probe across the whole array first. Wide ranges usually fail here, avoiding a full min/max scan before falling back to sorting.
            min = keys[0];
            char max = keys[0];
            int sampleCount = Math.Min(keys.Length, MappedRangeProbeSampleCount);

            for (int i = 1; i < sampleCount; i++)
            {
                char key = keys[GetSampleIndex(i, sampleCount, keys.Length - 1)];

                if (key < min)
                    min = key;
                else if (key > max)
                    max = key;

                if (max - min >= MaxMappedRange)
                {
                    range = 0;
                    return false;
                }
            }

            // If the probe did not reject the range, compute the exact bounds before allocating the seen map.
            if (sampleCount != keys.Length)
            {
                GetCharMinMax(keys, out min, out max);

                if (max - min >= MaxMappedRange)
                {
                    range = 0;
                    return false;
                }
            }

            range = max - min + 1;
            return true;
        }

        static int DeduplicateChar(Span<char> keys, int min, int range)
        {
            Span<byte> seen = stackalloc byte[range];
            seen.Clear();

            ref char keysRef = ref keys[0];
            ref byte seenRef = ref seen[0];

            for (int i = 0; i < keys.Length; i++)
            {
                char key = Unsafe.Add(ref keysRef, i);
                int index = key - min;
                ref byte seenValue = ref Unsafe.Add(ref seenRef, index);

                if (seenValue != 0)
                    continue;

                seenValue = 1;
            }

            int writeIndex = 0;

            for (int i = 0; i < range; i++)
            {
                if (Unsafe.Add(ref seenRef, i) == 0)
                    continue;

                Unsafe.Add(ref keysRef, writeIndex) = (char)(i + min);
                writeIndex++;
            }

            return writeIndex;
        }
    }

    private static bool TryDeduplicateInt16<TValue>(Span<short> keys, Span<TValue> values, out int uniqueCount)
    {
        if (!TryGetInt16MappedRange(keys, out short min, out int range) || !IsMappedRangeUsable(keys.Length, values.Length, range))
        {
            uniqueCount = 0;
            return false;
        }

        if (values.Length == 0)
        {
            uniqueCount = DeduplicateInt16(keys, min, range);
            return true;
        }

        Span<byte> seen = stackalloc byte[range];
        seen.Clear();
        Span<TValue> valueMap = new TValue[range];

        ref short keysRef = ref keys[0];
        ref TValue valuesRef = ref values[0];
        ref byte seenRef = ref seen[0];
        ref TValue valueMapRef = ref valueMap[0];

        for (int i = 0; i < keys.Length; i++)
        {
            short key = Unsafe.Add(ref keysRef, i);
            int index = key - min;
            ref byte seenValue = ref Unsafe.Add(ref seenRef, index);

            if (seenValue != 0)
                continue;

            seenValue = 1;
            Unsafe.Add(ref valueMapRef, index) = Unsafe.Add(ref valuesRef, i);
        }

        int writeIndex = 0;

        for (int i = 0; i < range; i++)
        {
            if (Unsafe.Add(ref seenRef, i) == 0)
                continue;

            Unsafe.Add(ref keysRef, writeIndex) = (short)(i + min);
            Unsafe.Add(ref valuesRef, writeIndex) = Unsafe.Add(ref valueMapRef, i);
            writeIndex++;
        }

        uniqueCount = writeIndex;
        return true;

        static bool TryGetInt16MappedRange(ReadOnlySpan<short> keys, out short min, out int range)
        {
            min = keys[0];
            short max = keys[0];
            int sampleCount = Math.Min(keys.Length, MappedRangeProbeSampleCount);

            for (int i = 1; i < sampleCount; i++)
            {
                short key = keys[GetSampleIndex(i, sampleCount, keys.Length - 1)];

                if (key < min)
                    min = key;
                else if (key > max)
                    max = key;

                if (max - min >= MaxMappedRange)
                {
                    range = 0;
                    return false;
                }
            }

            if (sampleCount != keys.Length)
            {
                GetInt16MinMax(keys, out min, out max);

                if (max - min >= MaxMappedRange)
                {
                    range = 0;
                    return false;
                }
            }

            range = max - min + 1;
            return true;
        }

        static int DeduplicateInt16(Span<short> keys, int min, int range)
        {
            Span<byte> seen = stackalloc byte[range];
            seen.Clear();

            ref short keysRef = ref keys[0];
            ref byte seenRef = ref seen[0];

            for (int i = 0; i < keys.Length; i++)
            {
                short key = Unsafe.Add(ref keysRef, i);
                int index = key - min;
                ref byte seenValue = ref Unsafe.Add(ref seenRef, index);

                if (seenValue != 0)
                    continue;

                seenValue = 1;
            }

            int writeIndex = 0;

            for (int i = 0; i < range; i++)
            {
                if (Unsafe.Add(ref seenRef, i) == 0)
                    continue;

                Unsafe.Add(ref keysRef, writeIndex) = (short)(i + min);
                writeIndex++;
            }

            return writeIndex;
        }
    }

    private static bool TryDeduplicateUInt16<TValue>(Span<ushort> keys, Span<TValue> values, out int uniqueCount)
    {
        if (!TryGetUInt16MappedRange(keys, out ushort min, out int range) || !IsMappedRangeUsable(keys.Length, values.Length, range))
        {
            uniqueCount = 0;
            return false;
        }

        if (values.Length == 0)
        {
            uniqueCount = DeduplicateUInt16(keys, min, range);
            return true;
        }

        Span<byte> seen = stackalloc byte[range];
        seen.Clear();
        Span<TValue> valueMap = new TValue[range];

        ref ushort keysRef = ref keys[0];
        ref TValue valuesRef = ref values[0];
        ref byte seenRef = ref seen[0];
        ref TValue valueMapRef = ref valueMap[0];

        for (int i = 0; i < keys.Length; i++)
        {
            ushort key = Unsafe.Add(ref keysRef, i);
            int index = key - min;
            ref byte seenValue = ref Unsafe.Add(ref seenRef, index);

            if (seenValue != 0)
                continue;

            seenValue = 1;
            Unsafe.Add(ref valueMapRef, index) = Unsafe.Add(ref valuesRef, i);
        }

        int writeIndex = 0;

        for (int i = 0; i < range; i++)
        {
            if (Unsafe.Add(ref seenRef, i) == 0)
                continue;

            Unsafe.Add(ref keysRef, writeIndex) = (ushort)(i + min);
            Unsafe.Add(ref valuesRef, writeIndex) = Unsafe.Add(ref valueMapRef, i);
            writeIndex++;
        }

        uniqueCount = writeIndex;
        return true;

        static bool TryGetUInt16MappedRange(ReadOnlySpan<ushort> keys, out ushort min, out int range)
        {
            min = keys[0];
            ushort max = keys[0];
            int sampleCount = Math.Min(keys.Length, MappedRangeProbeSampleCount);

            for (int i = 1; i < sampleCount; i++)
            {
                ushort key = keys[GetSampleIndex(i, sampleCount, keys.Length - 1)];

                if (key < min)
                    min = key;
                else if (key > max)
                    max = key;

                if (max - min >= MaxMappedRange)
                {
                    range = 0;
                    return false;
                }
            }

            if (sampleCount != keys.Length)
            {
                GetUInt16MinMax(keys, out min, out max);

                if (max - min >= MaxMappedRange)
                {
                    range = 0;
                    return false;
                }
            }

            range = max - min + 1;
            return true;
        }

        static int DeduplicateUInt16(Span<ushort> keys, int min, int range)
        {
            Span<byte> seen = stackalloc byte[range];
            seen.Clear();

            ref ushort keysRef = ref keys[0];
            ref byte seenRef = ref seen[0];

            for (int i = 0; i < keys.Length; i++)
            {
                ushort key = Unsafe.Add(ref keysRef, i);
                int index = key - min;
                ref byte seenValue = ref Unsafe.Add(ref seenRef, index);

                if (seenValue != 0)
                    continue;

                seenValue = 1;
            }

            int writeIndex = 0;

            for (int i = 0; i < range; i++)
            {
                if (Unsafe.Add(ref seenRef, i) == 0)
                    continue;

                Unsafe.Add(ref keysRef, writeIndex) = (ushort)(i + min);
                writeIndex++;
            }

            return writeIndex;
        }
    }

    private static bool TryDeduplicateInt32<TValue>(Span<int> keys, Span<TValue> values, out int uniqueCount)
    {
        if (!TryGetInt32MappedRange(keys, out int min, out int range) || !IsMappedRangeUsable(keys.Length, values.Length, range))
        {
            uniqueCount = 0;
            return false;
        }

        if (values.Length == 0)
        {
            uniqueCount = DeduplicateInt32(keys, min, range);
            return true;
        }

        Span<byte> seen = stackalloc byte[range];
        seen.Clear();
        Span<TValue> valueMap = new TValue[range];

        ref int keysRef = ref keys[0];
        ref TValue valuesRef = ref values[0];
        ref byte seenRef = ref seen[0];
        ref TValue valueMapRef = ref valueMap[0];

        for (int i = 0; i < keys.Length; i++)
        {
            int key = Unsafe.Add(ref keysRef, i);
            int index = key - min;
            ref byte seenValue = ref Unsafe.Add(ref seenRef, index);

            if (seenValue != 0)
                continue;

            seenValue = 1;
            Unsafe.Add(ref valueMapRef, index) = Unsafe.Add(ref valuesRef, i);
        }

        int writeIndex = 0;

        for (int i = 0; i < range; i++)
        {
            if (Unsafe.Add(ref seenRef, i) == 0)
                continue;

            Unsafe.Add(ref keysRef, writeIndex) = i + min;
            Unsafe.Add(ref valuesRef, writeIndex) = Unsafe.Add(ref valueMapRef, i);

            writeIndex++;
        }

        uniqueCount = writeIndex;
        return true;

        static bool TryGetInt32MappedRange(ReadOnlySpan<int> keys, out int min, out int range)
        {
            min = keys[0];
            int max = keys[0];
            int sampleCount = Math.Min(keys.Length, MappedRangeProbeSampleCount);

            for (int i = 1; i < sampleCount; i++)
            {
                int key = keys[GetSampleIndex(i, sampleCount, keys.Length - 1)];

                if (key < min)
                    min = key;
                else if (key > max)
                    max = key;

                if (unchecked((uint)(max - min)) >= MaxMappedRange)
                {
                    range = 0;
                    return false;
                }
            }

            if (sampleCount != keys.Length)
            {
                GetInt32MinMax(keys, out min, out max);

                if (unchecked((uint)(max - min)) >= MaxMappedRange)
                {
                    range = 0;
                    return false;
                }
            }

            range = unchecked((int)(uint)(max - min)) + 1;
            return true;
        }

        static int DeduplicateInt32(Span<int> keys, int min, int range)
        {
            Span<byte> seen = stackalloc byte[range];
            seen.Clear();

            ref int keysRef = ref keys[0];
            ref byte seenRef = ref seen[0];

            for (int i = 0; i < keys.Length; i++)
            {
                int key = Unsafe.Add(ref keysRef, i);
                int index = key - min;
                ref byte seenValue = ref Unsafe.Add(ref seenRef, index);

                if (seenValue != 0)
                    continue;

                seenValue = 1;
            }

            int writeIndex = 0;

            for (int i = 0; i < range; i++)
            {
                if (Unsafe.Add(ref seenRef, i) == 0)
                    continue;

                Unsafe.Add(ref keysRef, writeIndex) = i + min;
                writeIndex++;
            }

            return writeIndex;
        }
    }

    private static bool TryDeduplicateUInt32<TValue>(Span<uint> keys, Span<TValue> values, out int uniqueCount)
    {
        if (!TryGetUInt32MappedRange(keys, out uint min, out int range) || !IsMappedRangeUsable(keys.Length, values.Length, range))
        {
            uniqueCount = 0;
            return false;
        }

        if (values.Length == 0)
        {
            uniqueCount = DeduplicateUInt32(keys, min, range);
            return true;
        }

        Span<byte> seen = stackalloc byte[range];
        seen.Clear();
        Span<TValue> valueMap = new TValue[range];

        ref uint keysRef = ref keys[0];
        ref TValue valuesRef = ref values[0];
        ref byte seenRef = ref seen[0];
        ref TValue valueMapRef = ref valueMap[0];

        for (int i = 0; i < keys.Length; i++)
        {
            uint key = Unsafe.Add(ref keysRef, i);
            int index = (int)(key - min);
            ref byte seenValue = ref Unsafe.Add(ref seenRef, index);

            if (seenValue != 0)
                continue;

            seenValue = 1;
            Unsafe.Add(ref valueMapRef, index) = Unsafe.Add(ref valuesRef, i);
        }

        int writeIndex = 0;

        for (int i = 0; i < range; i++)
        {
            if (Unsafe.Add(ref seenRef, i) == 0)
                continue;

            Unsafe.Add(ref keysRef, writeIndex) = (uint)i + min;
            Unsafe.Add(ref valuesRef, writeIndex) = Unsafe.Add(ref valueMapRef, i);
            writeIndex++;
        }

        uniqueCount = writeIndex;
        return true;

        static bool TryGetUInt32MappedRange(ReadOnlySpan<uint> keys, out uint min, out int range)
        {
            min = keys[0];
            uint max = keys[0];
            int sampleCount = Math.Min(keys.Length, MappedRangeProbeSampleCount);

            for (int i = 1; i < sampleCount; i++)
            {
                uint key = keys[GetSampleIndex(i, sampleCount, keys.Length - 1)];

                if (key < min)
                    min = key;
                else if (key > max)
                    max = key;

                if (max - min >= MaxMappedRange)
                {
                    range = 0;
                    return false;
                }
            }

            if (sampleCount != keys.Length)
            {
                GetUInt32MinMax(keys, out min, out max);

                if (max - min >= MaxMappedRange)
                {
                    range = 0;
                    return false;
                }
            }

            range = (int)(max - min) + 1;
            return true;
        }

        static int DeduplicateUInt32(Span<uint> keys, uint min, int range)
        {
            Span<byte> seen = stackalloc byte[range];
            seen.Clear();

            ref uint keysRef = ref keys[0];
            ref byte seenRef = ref seen[0];

            for (int i = 0; i < keys.Length; i++)
            {
                uint key = Unsafe.Add(ref keysRef, i);
                int index = (int)(key - min);
                ref byte seenValue = ref Unsafe.Add(ref seenRef, index);

                if (seenValue != 0)
                    continue;

                seenValue = 1;
            }

            int writeIndex = 0;

            for (int i = 0; i < range; i++)
            {
                if (Unsafe.Add(ref seenRef, i) == 0)
                    continue;

                Unsafe.Add(ref keysRef, writeIndex) = (uint)i + min;
                writeIndex++;
            }

            return writeIndex;
        }
    }

    private static bool TryDeduplicateInt64<TValue>(Span<long> keys, Span<TValue> values, out int uniqueCount)
    {
        if (!TryGetInt64MappedRange(keys, out long min, out int range) || !IsMappedRangeUsable(keys.Length, values.Length, range))
        {
            uniqueCount = 0;
            return false;
        }

        if (values.Length == 0)
        {
            uniqueCount = DeduplicateInt64(keys, min, range);
            return true;
        }

        Span<byte> seen = stackalloc byte[range];
        seen.Clear();
        Span<TValue> valueMap = new TValue[range];

        ref long keysRef = ref keys[0];
        ref TValue valuesRef = ref values[0];
        ref byte seenRef = ref seen[0];
        ref TValue valueMapRef = ref valueMap[0];

        for (int i = 0; i < keys.Length; i++)
        {
            long key = Unsafe.Add(ref keysRef, i);
            int index = (int)(key - min);
            ref byte seenValue = ref Unsafe.Add(ref seenRef, index);

            if (seenValue != 0)
                continue;

            seenValue = 1;
            Unsafe.Add(ref valueMapRef, index) = Unsafe.Add(ref valuesRef, i);
        }

        int writeIndex = 0;

        for (int i = 0; i < range; i++)
        {
            if (Unsafe.Add(ref seenRef, i) == 0)
                continue;

            Unsafe.Add(ref keysRef, writeIndex) = i + min;
            Unsafe.Add(ref valuesRef, writeIndex) = Unsafe.Add(ref valueMapRef, i);
            writeIndex++;
        }

        uniqueCount = writeIndex;
        return true;

        static bool TryGetInt64MappedRange(ReadOnlySpan<long> keys, out long min, out int range)
        {
            min = keys[0];
            long max = keys[0];
            int sampleCount = Math.Min(keys.Length, MappedRangeProbeSampleCount);

            for (int i = 1; i < sampleCount; i++)
            {
                long key = keys[GetSampleIndex(i, sampleCount, keys.Length - 1)];

                if (key < min)
                    min = key;
                else if (key > max)
                    max = key;

                if (unchecked((ulong)max - (ulong)min) >= MaxMappedRange)
                {
                    range = 0;
                    return false;
                }
            }

            if (sampleCount != keys.Length)
            {
                GetInt64MinMax(keys, out min, out max);

                if (unchecked((ulong)max - (ulong)min) >= MaxMappedRange)
                {
                    range = 0;
                    return false;
                }
            }

            range = (int)unchecked((ulong)max - (ulong)min) + 1;
            return true;
        }

        static int DeduplicateInt64(Span<long> keys, long min, int range)
        {
            Span<byte> seen = stackalloc byte[range];
            seen.Clear();

            ref long keysRef = ref keys[0];
            ref byte seenRef = ref seen[0];

            for (int i = 0; i < keys.Length; i++)
            {
                long key = Unsafe.Add(ref keysRef, i);
                int index = (int)(key - min);
                ref byte seenValue = ref Unsafe.Add(ref seenRef, index);

                if (seenValue != 0)
                    continue;

                seenValue = 1;
            }

            int writeIndex = 0;

            for (int i = 0; i < range; i++)
            {
                if (Unsafe.Add(ref seenRef, i) == 0)
                    continue;

                Unsafe.Add(ref keysRef, writeIndex) = i + min;
                writeIndex++;
            }

            return writeIndex;
        }
    }

    private static bool TryDeduplicateUInt64<TValue>(Span<ulong> keys, Span<TValue> values, out int uniqueCount)
    {
        if (!TryGetUInt64MappedRange(keys, out ulong min, out int range) || !IsMappedRangeUsable(keys.Length, values.Length, range))
        {
            uniqueCount = 0;
            return false;
        }

        if (values.Length == 0)
        {
            uniqueCount = DeduplicateUInt64(keys, min, range);
            return true;
        }

        Span<byte> seen = stackalloc byte[range];
        seen.Clear();
        Span<TValue> valueMap = new TValue[range];

        ref ulong keysRef = ref keys[0];
        ref TValue valuesRef = ref values[0];
        ref byte seenRef = ref seen[0];
        ref TValue valueMapRef = ref valueMap[0];

        for (int i = 0; i < keys.Length; i++)
        {
            ulong key = Unsafe.Add(ref keysRef, i);
            int index = (int)(key - min);
            ref byte seenValue = ref Unsafe.Add(ref seenRef, index);

            if (seenValue != 0)
                continue;

            seenValue = 1;
            Unsafe.Add(ref valueMapRef, index) = Unsafe.Add(ref valuesRef, i);
        }

        int writeIndex = 0;

        for (int i = 0; i < range; i++)
        {
            if (Unsafe.Add(ref seenRef, i) == 0)
                continue;

            Unsafe.Add(ref keysRef, writeIndex) = (ulong)i + min;
            Unsafe.Add(ref valuesRef, writeIndex) = Unsafe.Add(ref valueMapRef, i);
            writeIndex++;
        }

        uniqueCount = writeIndex;
        return true;

        static bool TryGetUInt64MappedRange(ReadOnlySpan<ulong> keys, out ulong min, out int range)
        {
            min = keys[0];
            ulong max = keys[0];
            int sampleCount = Math.Min(keys.Length, MappedRangeProbeSampleCount);

            for (int i = 1; i < sampleCount; i++)
            {
                ulong key = keys[GetSampleIndex(i, sampleCount, keys.Length - 1)];

                if (key < min)
                    min = key;
                else if (key > max)
                    max = key;

                if (max - min >= MaxMappedRange)
                {
                    range = 0;
                    return false;
                }
            }

            if (sampleCount != keys.Length)
            {
                GetUInt64MinMax(keys, out min, out max);

                if (max - min >= MaxMappedRange)
                {
                    range = 0;
                    return false;
                }
            }

            range = (int)(max - min) + 1;
            return true;
        }

        static int DeduplicateUInt64(Span<ulong> keys, ulong min, int range)
        {
            Span<byte> seen = stackalloc byte[range];
            seen.Clear();

            ref ulong keysRef = ref keys[0];
            ref byte seenRef = ref seen[0];

            for (int i = 0; i < keys.Length; i++)
            {
                ulong key = Unsafe.Add(ref keysRef, i);
                int index = (int)(key - min);
                ref byte seenValue = ref Unsafe.Add(ref seenRef, index);

                if (seenValue != 0)
                    continue;

                seenValue = 1;
            }

            int writeIndex = 0;

            for (int i = 0; i < range; i++)
            {
                if (Unsafe.Add(ref seenRef, i) == 0)
                    continue;

                Unsafe.Add(ref keysRef, writeIndex) = (ulong)i + min;
                writeIndex++;
            }

            return writeIndex;
        }
    }

    private static void SortFallback<TKey, TValue>(TKey[] keys, TValue[] values, IComparer<TKey> comparer)
    {
        // For some reason, .NET does not have a fast path to avoid the virtual calls when the comparer is default,
        // so I've added one here.
        if (ReferenceEquals(comparer, Comparer<TKey>.Default))
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

    private static bool ShouldShrink(int length, int uniqueCount) => uniqueCount <= length / ShrinkThresholdDivisor;

    private static bool IsMappedRangeUsable(int keyCount, int valueCount, int range)
    {
        if (valueCount != 0 && range > MaxMappedRangeWithValues)
            return false;

        return (long)range <= (long)keyCount * MaxMappedRangeToKeyCountFactor;
    }

    private static T[] CopySlice<T>(ReadOnlySpan<T> data, int length)
    {
        T[] copy = new T[length];
        data.Slice(0, length).CopyTo(copy);
        return copy;
    }

    private static bool HasPigeonholeDuplicate<TKey>(int keyCount)
    {
        if (typeof(TKey) == typeof(byte) || typeof(TKey) == typeof(sbyte))
            return keyCount > ByteValueCount;

        if (typeof(TKey) == typeof(char) || typeof(TKey) == typeof(short) || typeof(TKey) == typeof(ushort))
            return keyCount > ushort.MaxValue + 1;

        return false;
    }

    // Spread samples from the first to the last item so clustered prefixes do not hide a wide range later in the input.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetSampleIndex(int sampleIndex, int sampleCount, int lastIndex) => (int)((long)sampleIndex * lastIndex / (sampleCount - 1));
}