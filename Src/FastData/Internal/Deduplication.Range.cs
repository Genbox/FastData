using System.Buffers;
using System.Runtime.CompilerServices;
using static Genbox.FastData.Internal.ValueHelper;

namespace Genbox.FastData.Internal;

internal static partial class Deduplication
{
    private static int DeduplicateUInt8<TValue>(Span<byte> keys, Span<TValue> values)
    {
        // Keep the value-aware loop branch-free for callers that provide values.
        if (values.Length == 0)
            return DeduplicateUInt8Keys(keys);

        Span<byte> seen = stackalloc byte[256];
        seen.Clear();

        // Map byte keys directly to their first value, then emit keys in byte order.
        TValue[] rentedValueMap = ArrayPool<TValue>.Shared.Rent(256);
        Span<TValue> valueMap = rentedValueMap.AsSpan(0, 256);

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

        for (int i = 0; i < 256; i++)
        {
            if (Unsafe.Add(ref seenRef, i) == 0)
                continue;

            Unsafe.Add(ref keysRef, writeIndex) = (byte)i;
            Unsafe.Add(ref valuesRef, writeIndex) = Unsafe.Add(ref valueMapRef, i);
            writeIndex++;
        }

        ArrayPool<TValue>.Shared.Return(rentedValueMap, true);
        return writeIndex;

        static int DeduplicateUInt8Keys(Span<byte> keys)
        {
            // A byte key can be used directly as an index into the seen map.
            Span<byte> seen = stackalloc byte[256];
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
            for (int i = 0; i < 256; i++)
            {
                if (Unsafe.Add(ref seenRef, i) == 0)
                    continue;

                Unsafe.Add(ref keysRef, writeIndex) = (byte)i;
                writeIndex++;
            }

            return writeIndex;
        }
    }

    private static int DeduplicateInt8<TValue>(Span<sbyte> keys, Span<TValue> values)
    {
        if (values.Length == 0)
            return DeduplicateInt8Keys(keys);

        Span<byte> seen = stackalloc byte[256];
        seen.Clear();

        // Bias signed bytes into sorted index order: -128..127 maps to 0..255.
        TValue[] rentedValueMap = ArrayPool<TValue>.Shared.Rent(256);
        Span<TValue> valueMap = rentedValueMap.AsSpan(0, 256);

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

        for (int i = 0; i < 256; i++)
        {
            if (Unsafe.Add(ref seenRef, i) == 0)
                continue;

            Unsafe.Add(ref keysRef, writeIndex) = (sbyte)(i + sbyte.MinValue);
            Unsafe.Add(ref valuesRef, writeIndex) = Unsafe.Add(ref valueMapRef, i);
            writeIndex++;
        }

        ArrayPool<TValue>.Shared.Return(rentedValueMap, true);
        return writeIndex;

        static int DeduplicateInt8Keys(Span<sbyte> keys)
        {
            Span<byte> seen = stackalloc byte[256];
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

            for (int i = 0; i < 256; i++)
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
        TValue[] rentedValueMap = ArrayPool<TValue>.Shared.Rent(range);
        Span<TValue> valueMap = rentedValueMap.AsSpan(0, range);

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

        ArrayPool<TValue>.Shared.Return(rentedValueMap, true);
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
        TValue[] rentedValueMap = ArrayPool<TValue>.Shared.Rent(range);
        Span<TValue> valueMap = rentedValueMap.AsSpan(0, range);

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

        ArrayPool<TValue>.Shared.Return(rentedValueMap, true);
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
        TValue[] rentedValueMap = ArrayPool<TValue>.Shared.Rent(range);
        Span<TValue> valueMap = rentedValueMap.AsSpan(0, range);

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

        ArrayPool<TValue>.Shared.Return(rentedValueMap, true);
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
        TValue[] rentedValueMap = ArrayPool<TValue>.Shared.Rent(range);
        Span<TValue> valueMap = rentedValueMap.AsSpan(0, range);

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

        ArrayPool<TValue>.Shared.Return(rentedValueMap, true);
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
        TValue[] rentedValueMap = ArrayPool<TValue>.Shared.Rent(range);
        Span<TValue> valueMap = rentedValueMap.AsSpan(0, range);

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

        ArrayPool<TValue>.Shared.Return(rentedValueMap, true);
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
        TValue[] rentedValueMap = ArrayPool<TValue>.Shared.Rent(range);
        Span<TValue> valueMap = rentedValueMap.AsSpan(0, range);

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

        ArrayPool<TValue>.Shared.Return(rentedValueMap, true);
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
        TValue[] rentedValueMap = ArrayPool<TValue>.Shared.Rent(range);
        Span<TValue> valueMap = rentedValueMap.AsSpan(0, range);

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

        ArrayPool<TValue>.Shared.Return(rentedValueMap, true);
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
}