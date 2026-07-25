using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Genbox.FastData.Internal;

internal static class ValueHelper
{
    internal static void GetCharMinMax(ReadOnlySpan<char> keys, out char min, out char max)
    {
        int length = keys.Length;
        int vectorSize = Vector<ushort>.Count;

        if (!Vector.IsHardwareAccelerated || length < vectorSize * 2)
        {
            GetCharMinMaxScalar(keys, out min, out max);
            return;
        }

        // Vector<T> does not support char, but char ordering matches its UTF-16 code-unit value.
        ReadOnlySpan<ushort> span = MemoryMarshal.Cast<char, ushort>(keys);
        ref ushort keyValuesRef = ref MemoryMarshal.GetReference(span);

        Vector<ushort> min0 = LoadVector(ref keyValuesRef, 0);
        Vector<ushort> max0 = min0;

        Vector<ushort> min1 = LoadVector(ref keyValuesRef, vectorSize);
        Vector<ushort> max1 = min1;

        int i = vectorSize * 2;
        int step = vectorSize * 2;

        for (; i <= length - step; i += step)
        {
            Vector<ushort> v0 = LoadVector(ref keyValuesRef, i);
            Vector<ushort> v1 = LoadVector(ref keyValuesRef, i + vectorSize);

            min0 = Vector.Min(min0, v0);
            max0 = Vector.Max(max0, v0);

            min1 = Vector.Min(min1, v1);
            max1 = Vector.Max(max1, v1);
        }

        Vector<ushort> minVector = Vector.Min(min0, min1);
        Vector<ushort> maxVector = Vector.Max(max0, max1);

        for (; i <= length - vectorSize; i += vectorSize)
        {
            Vector<ushort> v = LoadVector(ref keyValuesRef, i);

            minVector = Vector.Min(minVector, v);
            maxVector = Vector.Max(maxVector, v);
        }

        if (i < length)
        {
            Vector<ushort> tail = LoadVector(ref keyValuesRef, length - vectorSize);

            minVector = Vector.Min(minVector, tail);
            maxVector = Vector.Max(maxVector, tail);
        }

        ushort localMin = minVector[0];
        ushort localMax = maxVector[0];

        for (int j = 1; j < vectorSize; j++)
        {
            ushort minCandidate = minVector[j];
            ushort maxCandidate = maxVector[j];

            if (minCandidate < localMin)
                localMin = minCandidate;

            if (maxCandidate > localMax)
                localMax = maxCandidate;
        }

        min = (char)localMin;
        max = (char)localMax;

        static void GetCharMinMaxScalar(ReadOnlySpan<char> keys, out char min, out char max)
        {
            ref char keysRef = ref MemoryMarshal.GetReference(keys);
            min = keysRef;
            max = keysRef;

            for (int i = 1; i < keys.Length; i++)
            {
                char key = Unsafe.Add(ref keysRef, i);

                if (key < min)
                    min = key;

                if (key > max)
                    max = key;
            }
        }
    }

    internal static void GetInt16MinMax(ReadOnlySpan<short> keys, out short min, out short max)
    {
        int length = keys.Length;
        int vectorSize = Vector<short>.Count;

        if (!Vector.IsHardwareAccelerated || length < vectorSize * 2)
        {
            GetInt16MinMaxScalar(keys, out min, out max);
            return;
        }

        ref short keysRef = ref MemoryMarshal.GetReference(keys);

        Vector<short> min0 = LoadVector(ref keysRef, 0);
        Vector<short> max0 = min0;

        Vector<short> min1 = LoadVector(ref keysRef, vectorSize);
        Vector<short> max1 = min1;

        int i = vectorSize * 2;
        int step = vectorSize * 2;

        for (; i <= length - step; i += step)
        {
            Vector<short> v0 = LoadVector(ref keysRef, i);
            Vector<short> v1 = LoadVector(ref keysRef, i + vectorSize);

            min0 = Vector.Min(min0, v0);
            max0 = Vector.Max(max0, v0);

            min1 = Vector.Min(min1, v1);
            max1 = Vector.Max(max1, v1);
        }

        Vector<short> minVector = Vector.Min(min0, min1);
        Vector<short> maxVector = Vector.Max(max0, max1);

        for (; i <= length - vectorSize; i += vectorSize)
        {
            Vector<short> v = LoadVector(ref keysRef, i);

            minVector = Vector.Min(minVector, v);
            maxVector = Vector.Max(maxVector, v);
        }

        if (i < length)
        {
            Vector<short> tail = LoadVector(ref keysRef, length - vectorSize);

            minVector = Vector.Min(minVector, tail);
            maxVector = Vector.Max(maxVector, tail);
        }

        short localMin = minVector[0];
        short localMax = maxVector[0];

        for (int j = 1; j < vectorSize; j++)
        {
            short minCandidate = minVector[j];
            short maxCandidate = maxVector[j];

            if (minCandidate < localMin)
                localMin = minCandidate;

            if (maxCandidate > localMax)
                localMax = maxCandidate;
        }

        min = localMin;
        max = localMax;

        static void GetInt16MinMaxScalar(ReadOnlySpan<short> keys, out short min, out short max)
        {
            ref short keysRef = ref MemoryMarshal.GetReference(keys);
            min = keysRef;
            max = keysRef;

            for (int i = 1; i < keys.Length; i++)
            {
                short key = Unsafe.Add(ref keysRef, i);

                if (key < min)
                    min = key;

                if (key > max)
                    max = key;
            }
        }
    }

    internal static void GetUInt16MinMax(ReadOnlySpan<ushort> keys, out ushort min, out ushort max)
    {
        int length = keys.Length;
        int vectorSize = Vector<ushort>.Count;

        if (!Vector.IsHardwareAccelerated || length < vectorSize * 2)
        {
            GetUInt16MinMaxScalar(keys, out min, out max);
            return;
        }

        ref ushort keysRef = ref MemoryMarshal.GetReference(keys);

        Vector<ushort> min0 = LoadVector(ref keysRef, 0);
        Vector<ushort> max0 = min0;

        Vector<ushort> min1 = LoadVector(ref keysRef, vectorSize);
        Vector<ushort> max1 = min1;

        int i = vectorSize * 2;
        int step = vectorSize * 2;

        for (; i <= length - step; i += step)
        {
            Vector<ushort> v0 = LoadVector(ref keysRef, i);
            Vector<ushort> v1 = LoadVector(ref keysRef, i + vectorSize);

            min0 = Vector.Min(min0, v0);
            max0 = Vector.Max(max0, v0);

            min1 = Vector.Min(min1, v1);
            max1 = Vector.Max(max1, v1);
        }

        Vector<ushort> minVector = Vector.Min(min0, min1);
        Vector<ushort> maxVector = Vector.Max(max0, max1);

        for (; i <= length - vectorSize; i += vectorSize)
        {
            Vector<ushort> v = LoadVector(ref keysRef, i);

            minVector = Vector.Min(minVector, v);
            maxVector = Vector.Max(maxVector, v);
        }

        if (i < length)
        {
            Vector<ushort> tail = LoadVector(ref keysRef, length - vectorSize);

            minVector = Vector.Min(minVector, tail);
            maxVector = Vector.Max(maxVector, tail);
        }

        ushort localMin = minVector[0];
        ushort localMax = maxVector[0];

        for (int j = 1; j < vectorSize; j++)
        {
            ushort minCandidate = minVector[j];
            ushort maxCandidate = maxVector[j];

            if (minCandidate < localMin)
                localMin = minCandidate;

            if (maxCandidate > localMax)
                localMax = maxCandidate;
        }

        min = localMin;
        max = localMax;

        static void GetUInt16MinMaxScalar(ReadOnlySpan<ushort> keys, out ushort min, out ushort max)
        {
            ref ushort keysRef = ref MemoryMarshal.GetReference(keys);
            min = keysRef;
            max = keysRef;

            for (int i = 1; i < keys.Length; i++)
            {
                ushort key = Unsafe.Add(ref keysRef, i);

                if (key < min)
                    min = key;

                if (key > max)
                    max = key;
            }
        }
    }

    internal static void GetInt32MinMax(ReadOnlySpan<int> keys, out int min, out int max)
    {
        int length = keys.Length;
        int vectorSize = Vector<int>.Count;

        if (!Vector.IsHardwareAccelerated || length < vectorSize * 2)
        {
            GetInt32MinMaxScalar(keys, out min, out max);
            return;
        }

        ref int keysRef = ref MemoryMarshal.GetReference(keys);

        Vector<int> min0 = LoadVector(ref keysRef, 0);
        Vector<int> max0 = min0;

        Vector<int> min1 = LoadVector(ref keysRef, vectorSize);
        Vector<int> max1 = min1;

        int i = vectorSize * 2;
        int step = vectorSize * 2;

        for (; i <= length - step; i += step)
        {
            Vector<int> v0 = LoadVector(ref keysRef, i);
            Vector<int> v1 = LoadVector(ref keysRef, i + vectorSize);

            min0 = Vector.Min(min0, v0);
            max0 = Vector.Max(max0, v0);

            min1 = Vector.Min(min1, v1);
            max1 = Vector.Max(max1, v1);
        }

        Vector<int> minVector = Vector.Min(min0, min1);
        Vector<int> maxVector = Vector.Max(max0, max1);

        for (; i <= length - vectorSize; i += vectorSize)
        {
            Vector<int> v = LoadVector(ref keysRef, i);

            minVector = Vector.Min(minVector, v);
            maxVector = Vector.Max(maxVector, v);
        }

        if (i < length)
        {
            Vector<int> tail = LoadVector(ref keysRef, length - vectorSize);

            minVector = Vector.Min(minVector, tail);
            maxVector = Vector.Max(maxVector, tail);
        }

        int localMin = minVector[0];
        int localMax = maxVector[0];

        for (int j = 1; j < vectorSize; j++)
        {
            int minCandidate = minVector[j];
            int maxCandidate = maxVector[j];

            if (minCandidate < localMin)
                localMin = minCandidate;

            if (maxCandidate > localMax)
                localMax = maxCandidate;
        }

        min = localMin;
        max = localMax;

        static void GetInt32MinMaxScalar(ReadOnlySpan<int> keys, out int min, out int max)
        {
            ref int keysRef = ref MemoryMarshal.GetReference(keys);
            min = keysRef;
            max = keysRef;

            for (int i = 1; i < keys.Length; i++)
            {
                int key = Unsafe.Add(ref keysRef, i);

                if (key < min)
                    min = key;

                if (key > max)
                    max = key;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector<T> LoadVector<T>(ref T source, int elementOffset)
        where T : struct => Unsafe.ReadUnaligned<Vector<T>>(
        ref Unsafe.As<T, byte>(ref Unsafe.Add(ref source, elementOffset)));

    internal static void GetUInt32MinMax(ReadOnlySpan<uint> keys, out uint min, out uint max)
    {
        int length = keys.Length;
        int vectorSize = Vector<uint>.Count;

        if (!Vector.IsHardwareAccelerated || length < vectorSize * 2)
        {
            GetUInt32MinMaxScalar(keys, out min, out max);
            return;
        }

        ref uint keysRef = ref MemoryMarshal.GetReference(keys);

        Vector<uint> min0 = LoadVector(ref keysRef, 0);
        Vector<uint> max0 = min0;

        Vector<uint> min1 = LoadVector(ref keysRef, vectorSize);
        Vector<uint> max1 = min1;

        int i = vectorSize * 2;
        int step = vectorSize * 2;

        for (; i <= length - step; i += step)
        {
            Vector<uint> v0 = LoadVector(ref keysRef, i);
            Vector<uint> v1 = LoadVector(ref keysRef, i + vectorSize);

            min0 = Vector.Min(min0, v0);
            max0 = Vector.Max(max0, v0);

            min1 = Vector.Min(min1, v1);
            max1 = Vector.Max(max1, v1);
        }

        Vector<uint> minVector = Vector.Min(min0, min1);
        Vector<uint> maxVector = Vector.Max(max0, max1);

        for (; i <= length - vectorSize; i += vectorSize)
        {
            Vector<uint> v = LoadVector(ref keysRef, i);

            minVector = Vector.Min(minVector, v);
            maxVector = Vector.Max(maxVector, v);
        }

        if (i < length)
        {
            Vector<uint> tail = LoadVector(ref keysRef, length - vectorSize);

            minVector = Vector.Min(minVector, tail);
            maxVector = Vector.Max(maxVector, tail);
        }

        uint localMin = minVector[0];
        uint localMax = maxVector[0];

        for (int j = 1; j < vectorSize; j++)
        {
            uint minCandidate = minVector[j];
            uint maxCandidate = maxVector[j];

            if (minCandidate < localMin)
                localMin = minCandidate;

            if (maxCandidate > localMax)
                localMax = maxCandidate;
        }

        min = localMin;
        max = localMax;

        static void GetUInt32MinMaxScalar(ReadOnlySpan<uint> keys, out uint min, out uint max)
        {
            ref uint keysRef = ref MemoryMarshal.GetReference(keys);
            min = keysRef;
            max = keysRef;

            for (int i = 1; i < keys.Length; i++)
            {
                uint key = Unsafe.Add(ref keysRef, i);

                if (key < min)
                    min = key;

                if (key > max)
                    max = key;
            }
        }
    }

    internal static void GetInt64MinMax(ReadOnlySpan<long> keys, out long min, out long max)
    {
        int length = keys.Length;
        int vectorSize = Vector<long>.Count;

        if (!Vector.IsHardwareAccelerated || length < vectorSize * 2)
        {
            GetInt64MinMaxScalar(keys, out min, out max);
            return;
        }

        ref long keysRef = ref MemoryMarshal.GetReference(keys);

        Vector<long> min0 = LoadVector(ref keysRef, 0);
        Vector<long> max0 = min0;

        Vector<long> min1 = LoadVector(ref keysRef, vectorSize);
        Vector<long> max1 = min1;

        int i = vectorSize * 2;
        int step = vectorSize * 2;

        for (; i <= length - step; i += step)
        {
            Vector<long> v0 = LoadVector(ref keysRef, i);
            Vector<long> v1 = LoadVector(ref keysRef, i + vectorSize);

            min0 = Vector.Min(min0, v0);
            max0 = Vector.Max(max0, v0);

            min1 = Vector.Min(min1, v1);
            max1 = Vector.Max(max1, v1);
        }

        Vector<long> minVector = Vector.Min(min0, min1);
        Vector<long> maxVector = Vector.Max(max0, max1);

        for (; i <= length - vectorSize; i += vectorSize)
        {
            Vector<long> v = LoadVector(ref keysRef, i);

            minVector = Vector.Min(minVector, v);
            maxVector = Vector.Max(maxVector, v);
        }

        if (i < length)
        {
            Vector<long> tail = LoadVector(ref keysRef, length - vectorSize);

            minVector = Vector.Min(minVector, tail);
            maxVector = Vector.Max(maxVector, tail);
        }

        long localMin = minVector[0];
        long localMax = maxVector[0];

        for (int j = 1; j < vectorSize; j++)
        {
            long minCandidate = minVector[j];
            long maxCandidate = maxVector[j];

            if (minCandidate < localMin)
                localMin = minCandidate;

            if (maxCandidate > localMax)
                localMax = maxCandidate;
        }

        min = localMin;
        max = localMax;

        static void GetInt64MinMaxScalar(ReadOnlySpan<long> keys, out long min, out long max)
        {
            ref long keysRef = ref MemoryMarshal.GetReference(keys);
            min = keysRef;
            max = keysRef;

            for (int i = 1; i < keys.Length; i++)
            {
                long key = Unsafe.Add(ref keysRef, i);

                if (key < min)
                    min = key;

                if (key > max)
                    max = key;
            }
        }
    }

    internal static void GetUInt64MinMax(ReadOnlySpan<ulong> keys, out ulong min, out ulong max)
    {
        int length = keys.Length;
        int vectorSize = Vector<ulong>.Count;

        if (!Vector.IsHardwareAccelerated || length < vectorSize * 2)
        {
            GetUInt64MinMaxScalar(keys, out min, out max);
            return;
        }

        ref ulong keysRef = ref MemoryMarshal.GetReference(keys);

        Vector<ulong> min0 = LoadVector(ref keysRef, 0);
        Vector<ulong> max0 = min0;

        Vector<ulong> min1 = LoadVector(ref keysRef, vectorSize);
        Vector<ulong> max1 = min1;

        int i = vectorSize * 2;
        int step = vectorSize * 2;

        for (; i <= length - step; i += step)
        {
            Vector<ulong> v0 = LoadVector(ref keysRef, i);
            Vector<ulong> v1 = LoadVector(ref keysRef, i + vectorSize);

            min0 = Vector.Min(min0, v0);
            max0 = Vector.Max(max0, v0);

            min1 = Vector.Min(min1, v1);
            max1 = Vector.Max(max1, v1);
        }

        Vector<ulong> minVector = Vector.Min(min0, min1);
        Vector<ulong> maxVector = Vector.Max(max0, max1);

        for (; i <= length - vectorSize; i += vectorSize)
        {
            Vector<ulong> v = LoadVector(ref keysRef, i);

            minVector = Vector.Min(minVector, v);
            maxVector = Vector.Max(maxVector, v);
        }

        if (i < length)
        {
            Vector<ulong> tail = LoadVector(ref keysRef, length - vectorSize);

            minVector = Vector.Min(minVector, tail);
            maxVector = Vector.Max(maxVector, tail);
        }

        ulong localMin = minVector[0];
        ulong localMax = maxVector[0];

        for (int j = 1; j < vectorSize; j++)
        {
            ulong minCandidate = minVector[j];
            ulong maxCandidate = maxVector[j];

            if (minCandidate < localMin)
                localMin = minCandidate;

            if (maxCandidate > localMax)
                localMax = maxCandidate;
        }

        min = localMin;
        max = localMax;

        static void GetUInt64MinMaxScalar(ReadOnlySpan<ulong> keys, out ulong min, out ulong max)
        {
            ref ulong keysRef = ref MemoryMarshal.GetReference(keys);
            min = keysRef;
            max = keysRef;

            for (int i = 1; i < keys.Length; i++)
            {
                ulong key = Unsafe.Add(ref keysRef, i);

                if (key < min)
                    min = key;

                if (key > max)
                    max = key;
            }
        }
    }
}