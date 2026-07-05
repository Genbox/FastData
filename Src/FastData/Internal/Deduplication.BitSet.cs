using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using static Genbox.FastData.Internal.ValueHelper;

namespace Genbox.FastData.Internal;

internal static partial class Deduplication
{
    private static bool TryDeduplicateInt32BitSet<TValue>(Span<int> keys, Span<TValue> values, out int uniqueCount)
    {
        if (!TryGetInt32BitSetRange(keys, out int min, out ulong range))
        {
            uniqueCount = 0;
            return false;
        }

        int seenLength = (int)((range + 63) >> 6);
        ulong[] rentedSeen = ArrayPool<ulong>.Shared.Rent(seenLength);
        Span<ulong> seen = rentedSeen.AsSpan(0, seenLength);
        seen.Clear();

        if (values.Length == 0)
        {
            uniqueCount = DeduplicateInt32BitSet(keys, min, seen);
            ArrayPool<ulong>.Shared.Return(rentedSeen);
            return true;
        }

        TValue[] rentedValueMap = ArrayPool<TValue>.Shared.Rent((int)range);
        Span<TValue> valueMap = rentedValueMap.AsSpan(0, (int)range);

        ref int keysRef = ref keys[0];
        ref TValue valuesRef = ref values[0];
        ref ulong seenRef = ref seen[0];
        ref TValue valueMapRef = ref valueMap[0];

        for (int i = 0; i < keys.Length; i++)
        {
            int index = Unsafe.Add(ref keysRef, i) - min;
            ulong mask = 1UL << (index & 63);
            ref ulong seenWord = ref Unsafe.Add(ref seenRef, index >> 6);

            if ((seenWord & mask) == 0)
            {
                seenWord |= mask;
                Unsafe.Add(ref valueMapRef, index) = Unsafe.Add(ref valuesRef, i);
            }
        }

        int writeIndex = 0;

        for (int wordIndex = 0; wordIndex < seen.Length; wordIndex++)
        {
            ulong word = Unsafe.Add(ref seenRef, wordIndex);

            while (word != 0)
            {
                int bit = BitOperations.TrailingZeroCount(word);
                int keyIndex = (wordIndex << 6) + bit;
                Unsafe.Add(ref keysRef, writeIndex) = keyIndex + min;
                Unsafe.Add(ref valuesRef, writeIndex) = Unsafe.Add(ref valueMapRef, keyIndex);
                writeIndex++;
                word &= word - 1;
            }
        }

        ArrayPool<TValue>.Shared.Return(rentedValueMap, true);
        ArrayPool<ulong>.Shared.Return(rentedSeen);
        uniqueCount = writeIndex;
        return true;

        static bool TryGetInt32BitSetRange(ReadOnlySpan<int> keys, out int min, out ulong range)
        {
            if (!TryProbeInt32BitSet(keys))
            {
                min = 0;
                range = 0;
                return false;
            }

            GetInt32MinMax(keys, out min, out int max);
            range = unchecked((uint)(max - min)) + 1UL;
            return IsBitSetRangeUsable(keys.Length, range);
        }

        static bool TryProbeInt32BitSet(ReadOnlySpan<int> keys)
        {
            int min = keys[0];
            int max = keys[0];
            int sampleCount = Math.Min(keys.Length, MappedRangeProbeSampleCount);

            for (int i = 1; i < sampleCount; i++)
            {
                int key = keys[GetSampleIndex(i, sampleCount, keys.Length - 1)];

                if (key < min)
                    min = key;
                else if (key > max)
                    max = key;

                if (!IsBitSetRangeUsable(keys.Length, unchecked((uint)(max - min)) + 1UL))
                    return false;
            }

            return true;
        }

        static int DeduplicateInt32BitSet(Span<int> keys, int min, Span<ulong> seen)
        {
            ref int keysRef = ref keys[0];
            ref ulong seenRef = ref seen[0];

            for (int i = 0; i < keys.Length; i++)
            {
                int index = Unsafe.Add(ref keysRef, i) - min;
                Unsafe.Add(ref seenRef, index >> 6) |= 1UL << (index & 63);
            }

            int writeIndex = 0;

            for (int wordIndex = 0; wordIndex < seen.Length; wordIndex++)
            {
                ulong word = Unsafe.Add(ref seenRef, wordIndex);

                while (word != 0)
                {
                    int bit = BitOperations.TrailingZeroCount(word);
                    Unsafe.Add(ref keysRef, writeIndex) = (wordIndex << 6) + bit + min;
                    writeIndex++;
                    word &= word - 1;
                }
            }

            return writeIndex;
        }
    }

    private static bool TryDeduplicateUInt32BitSet<TValue>(Span<uint> keys, Span<TValue> values, out int uniqueCount)
    {
        if (!TryGetUInt32BitSetRange(keys, out uint min, out ulong range))
        {
            uniqueCount = 0;
            return false;
        }

        int seenLength = (int)((range + 63) >> 6);
        ulong[] rentedSeen = ArrayPool<ulong>.Shared.Rent(seenLength);
        Span<ulong> seen = rentedSeen.AsSpan(0, seenLength);
        seen.Clear();

        if (values.Length == 0)
        {
            uniqueCount = DeduplicateUInt32BitSet(keys, min, seen);
            ArrayPool<ulong>.Shared.Return(rentedSeen);
            return true;
        }

        TValue[] rentedValueMap = ArrayPool<TValue>.Shared.Rent((int)range);
        Span<TValue> valueMap = rentedValueMap.AsSpan(0, (int)range);

        ref uint keysRef = ref keys[0];
        ref TValue valuesRef = ref values[0];
        ref ulong seenRef = ref seen[0];
        ref TValue valueMapRef = ref valueMap[0];

        for (int i = 0; i < keys.Length; i++)
        {
            int index = (int)(Unsafe.Add(ref keysRef, i) - min);
            ulong mask = 1UL << (index & 63);
            ref ulong seenWord = ref Unsafe.Add(ref seenRef, index >> 6);

            if ((seenWord & mask) == 0)
            {
                seenWord |= mask;
                Unsafe.Add(ref valueMapRef, index) = Unsafe.Add(ref valuesRef, i);
            }
        }

        int writeIndex = 0;

        for (int wordIndex = 0; wordIndex < seen.Length; wordIndex++)
        {
            ulong word = Unsafe.Add(ref seenRef, wordIndex);

            while (word != 0)
            {
                int bit = BitOperations.TrailingZeroCount(word);
                int keyIndex = (wordIndex << 6) + bit;
                Unsafe.Add(ref keysRef, writeIndex) = (uint)keyIndex + min;
                Unsafe.Add(ref valuesRef, writeIndex) = Unsafe.Add(ref valueMapRef, keyIndex);
                writeIndex++;
                word &= word - 1;
            }
        }

        ArrayPool<TValue>.Shared.Return(rentedValueMap, true);
        ArrayPool<ulong>.Shared.Return(rentedSeen);
        uniqueCount = writeIndex;
        return true;

        static bool TryGetUInt32BitSetRange(ReadOnlySpan<uint> keys, out uint min, out ulong range)
        {
            if (!TryProbeUInt32BitSet(keys))
            {
                min = 0;
                range = 0;
                return false;
            }

            GetUInt32MinMax(keys, out min, out uint max);
            range = max - min + 1UL;
            return IsBitSetRangeUsable(keys.Length, range);
        }

        static bool TryProbeUInt32BitSet(ReadOnlySpan<uint> keys)
        {
            uint min = keys[0];
            uint max = keys[0];
            int sampleCount = Math.Min(keys.Length, MappedRangeProbeSampleCount);

            for (int i = 1; i < sampleCount; i++)
            {
                uint key = keys[GetSampleIndex(i, sampleCount, keys.Length - 1)];

                if (key < min)
                    min = key;
                else if (key > max)
                    max = key;

                if (!IsBitSetRangeUsable(keys.Length, max - min + 1UL))
                    return false;
            }

            return true;
        }

        static int DeduplicateUInt32BitSet(Span<uint> keys, uint min, Span<ulong> seen)
        {
            ref uint keysRef = ref keys[0];
            ref ulong seenRef = ref seen[0];

            for (int i = 0; i < keys.Length; i++)
            {
                int index = (int)(Unsafe.Add(ref keysRef, i) - min);
                Unsafe.Add(ref seenRef, index >> 6) |= 1UL << (index & 63);
            }

            int writeIndex = 0;

            for (int wordIndex = 0; wordIndex < seen.Length; wordIndex++)
            {
                ulong word = Unsafe.Add(ref seenRef, wordIndex);

                while (word != 0)
                {
                    int bit = BitOperations.TrailingZeroCount(word);
                    Unsafe.Add(ref keysRef, writeIndex) = (uint)((wordIndex << 6) + bit) + min;
                    writeIndex++;
                    word &= word - 1;
                }
            }

            return writeIndex;
        }
    }

    private static bool TryDeduplicateInt64BitSet<TValue>(Span<long> keys, Span<TValue> values, out int uniqueCount)
    {
        if (!TryGetInt64BitSetRange(keys, out long min, out ulong range))
        {
            uniqueCount = 0;
            return false;
        }

        int seenLength = (int)((range + 63) >> 6);
        ulong[] rentedSeen = ArrayPool<ulong>.Shared.Rent(seenLength);
        Span<ulong> seen = rentedSeen.AsSpan(0, seenLength);
        seen.Clear();

        if (values.Length == 0)
        {
            uniqueCount = DeduplicateInt64BitSet(keys, min, seen);
            ArrayPool<ulong>.Shared.Return(rentedSeen);
            return true;
        }

        TValue[] rentedValueMap = ArrayPool<TValue>.Shared.Rent((int)range);
        Span<TValue> valueMap = rentedValueMap.AsSpan(0, (int)range);

        ref long keysRef = ref keys[0];
        ref TValue valuesRef = ref values[0];
        ref ulong seenRef = ref seen[0];
        ref TValue valueMapRef = ref valueMap[0];

        for (int i = 0; i < keys.Length; i++)
        {
            int index = (int)(Unsafe.Add(ref keysRef, i) - min);
            ulong mask = 1UL << (index & 63);
            ref ulong seenWord = ref Unsafe.Add(ref seenRef, index >> 6);

            if ((seenWord & mask) == 0)
            {
                seenWord |= mask;
                Unsafe.Add(ref valueMapRef, index) = Unsafe.Add(ref valuesRef, i);
            }
        }

        int writeIndex = 0;

        for (int wordIndex = 0; wordIndex < seen.Length; wordIndex++)
        {
            ulong word = Unsafe.Add(ref seenRef, wordIndex);

            while (word != 0)
            {
                int bit = BitOperations.TrailingZeroCount(word);
                int keyIndex = (wordIndex << 6) + bit;
                Unsafe.Add(ref keysRef, writeIndex) = keyIndex + min;
                Unsafe.Add(ref valuesRef, writeIndex) = Unsafe.Add(ref valueMapRef, keyIndex);
                writeIndex++;
                word &= word - 1;
            }
        }

        ArrayPool<TValue>.Shared.Return(rentedValueMap, true);
        ArrayPool<ulong>.Shared.Return(rentedSeen);
        uniqueCount = writeIndex;
        return true;

        static bool TryGetInt64BitSetRange(ReadOnlySpan<long> keys, out long min, out ulong range)
        {
            if (!TryProbeInt64BitSet(keys))
            {
                min = 0;
                range = 0;
                return false;
            }

            GetInt64MinMax(keys, out min, out long max);
            ulong diff = unchecked((ulong)max - (ulong)min);

            if (diff == ulong.MaxValue)
            {
                range = 0;
                return false;
            }

            range = diff + 1;
            return IsBitSetRangeUsable(keys.Length, range);
        }

        static bool TryProbeInt64BitSet(ReadOnlySpan<long> keys)
        {
            long min = keys[0];
            long max = keys[0];
            int sampleCount = Math.Min(keys.Length, MappedRangeProbeSampleCount);

            for (int i = 1; i < sampleCount; i++)
            {
                long key = keys[GetSampleIndex(i, sampleCount, keys.Length - 1)];

                if (key < min)
                    min = key;
                else if (key > max)
                    max = key;

                ulong diff = unchecked((ulong)max - (ulong)min);

                if (diff == ulong.MaxValue || !IsBitSetRangeUsable(keys.Length, diff + 1))
                    return false;
            }

            return true;
        }

        static int DeduplicateInt64BitSet(Span<long> keys, long min, Span<ulong> seen)
        {
            ref long keysRef = ref keys[0];
            ref ulong seenRef = ref seen[0];

            for (int i = 0; i < keys.Length; i++)
            {
                int index = (int)(Unsafe.Add(ref keysRef, i) - min);
                Unsafe.Add(ref seenRef, index >> 6) |= 1UL << (index & 63);
            }

            int writeIndex = 0;

            for (int wordIndex = 0; wordIndex < seen.Length; wordIndex++)
            {
                ulong word = Unsafe.Add(ref seenRef, wordIndex);

                while (word != 0)
                {
                    int bit = BitOperations.TrailingZeroCount(word);
                    Unsafe.Add(ref keysRef, writeIndex) = (wordIndex << 6) + bit + min;
                    writeIndex++;
                    word &= word - 1;
                }
            }

            return writeIndex;
        }
    }

    private static bool TryDeduplicateUInt64BitSet<TValue>(Span<ulong> keys, Span<TValue> values, out int uniqueCount)
    {
        if (!TryGetUInt64BitSetRange(keys, out ulong min, out ulong range))
        {
            uniqueCount = 0;
            return false;
        }

        int seenLength = (int)((range + 63) >> 6);
        ulong[] rentedSeen = ArrayPool<ulong>.Shared.Rent(seenLength);
        Span<ulong> seen = rentedSeen.AsSpan(0, seenLength);
        seen.Clear();

        if (values.Length == 0)
        {
            uniqueCount = DeduplicateUInt64BitSet(keys, min, seen);
            ArrayPool<ulong>.Shared.Return(rentedSeen);
            return true;
        }

        TValue[] rentedValueMap = ArrayPool<TValue>.Shared.Rent((int)range);
        Span<TValue> valueMap = rentedValueMap.AsSpan(0, (int)range);

        ref ulong keysRef = ref keys[0];
        ref TValue valuesRef = ref values[0];
        ref ulong seenRef = ref seen[0];
        ref TValue valueMapRef = ref valueMap[0];

        for (int i = 0; i < keys.Length; i++)
        {
            int index = (int)(Unsafe.Add(ref keysRef, i) - min);
            ulong mask = 1UL << (index & 63);
            ref ulong seenWord = ref Unsafe.Add(ref seenRef, index >> 6);

            if ((seenWord & mask) == 0)
            {
                seenWord |= mask;
                Unsafe.Add(ref valueMapRef, index) = Unsafe.Add(ref valuesRef, i);
            }
        }

        int writeIndex = 0;

        for (int wordIndex = 0; wordIndex < seen.Length; wordIndex++)
        {
            ulong word = Unsafe.Add(ref seenRef, wordIndex);

            while (word != 0)
            {
                int bit = BitOperations.TrailingZeroCount(word);
                int keyIndex = (wordIndex << 6) + bit;
                Unsafe.Add(ref keysRef, writeIndex) = (ulong)keyIndex + min;
                Unsafe.Add(ref valuesRef, writeIndex) = Unsafe.Add(ref valueMapRef, keyIndex);
                writeIndex++;
                word &= word - 1;
            }
        }

        ArrayPool<TValue>.Shared.Return(rentedValueMap, true);
        ArrayPool<ulong>.Shared.Return(rentedSeen);
        uniqueCount = writeIndex;
        return true;

        static bool TryGetUInt64BitSetRange(ReadOnlySpan<ulong> keys, out ulong min, out ulong range)
        {
            if (!TryProbeUInt64BitSet(keys))
            {
                min = 0;
                range = 0;
                return false;
            }

            GetUInt64MinMax(keys, out min, out ulong max);
            ulong diff = max - min;

            if (diff == ulong.MaxValue)
            {
                range = 0;
                return false;
            }

            range = diff + 1;
            return IsBitSetRangeUsable(keys.Length, range);
        }

        static bool TryProbeUInt64BitSet(ReadOnlySpan<ulong> keys)
        {
            ulong min = keys[0];
            ulong max = keys[0];
            int sampleCount = Math.Min(keys.Length, MappedRangeProbeSampleCount);

            for (int i = 1; i < sampleCount; i++)
            {
                ulong key = keys[GetSampleIndex(i, sampleCount, keys.Length - 1)];

                if (key < min)
                    min = key;
                else if (key > max)
                    max = key;

                ulong diff = max - min;

                if (diff == ulong.MaxValue || !IsBitSetRangeUsable(keys.Length, diff + 1))
                    return false;
            }

            return true;
        }

        static int DeduplicateUInt64BitSet(Span<ulong> keys, ulong min, Span<ulong> seen)
        {
            ref ulong keysRef = ref keys[0];
            ref ulong seenRef = ref seen[0];

            for (int i = 0; i < keys.Length; i++)
            {
                int index = (int)(Unsafe.Add(ref keysRef, i) - min);
                Unsafe.Add(ref seenRef, index >> 6) |= 1UL << (index & 63);
            }

            int writeIndex = 0;

            for (int wordIndex = 0; wordIndex < seen.Length; wordIndex++)
            {
                ulong word = Unsafe.Add(ref seenRef, wordIndex);

                while (word != 0)
                {
                    int bit = BitOperations.TrailingZeroCount(word);
                    Unsafe.Add(ref keysRef, writeIndex) = (ulong)((wordIndex << 6) + bit) + min;
                    writeIndex++;
                    word &= word - 1;
                }
            }

            return writeIndex;
        }
    }
}