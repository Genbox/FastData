using System.Runtime.CompilerServices;

namespace Genbox.FastData.Internal;

internal static partial class Deduplication
{
    private const int MaxMappedRange = 65536;
    private const int MaxMappedRangeWithValues = 16 * 1024;
    private const int MaxMappedRangeToKeyCountFactor = 16;
    private const int MaxBitSetRange = 64 * 1024 * 1024;
    private const int MaxBitSetRangeToKeyCountFactor = 64;
    private const int MappedRangeProbeSampleCount = 64;
    private const int ShrinkThresholdDivisor = 2;

    private static bool ShouldShrink(int length, int uniqueCount) => uniqueCount <= length / ShrinkThresholdDivisor;

    private static bool IsMappedRangeUsable(int keyCount, int valueCount, int range)
    {
        if (valueCount != 0 && range > MaxMappedRangeWithValues)
            return false;

        return range <= (long)keyCount * MaxMappedRangeToKeyCountFactor;
    }

    private static bool ShouldUseBitSet(int keyCount) => (ulong)keyCount * MaxBitSetRangeToKeyCountFactor > MaxMappedRange;

    private static bool IsBitSetRangeUsable(int keyCount, ulong range)
    {
        if (range is 0 or > MaxBitSetRange)
            return false;

        return range <= (ulong)keyCount * MaxBitSetRangeToKeyCountFactor;
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
            return keyCount > 256;

        if (typeof(TKey) == typeof(char) || typeof(TKey) == typeof(short) || typeof(TKey) == typeof(ushort))
            return keyCount > 65536;

        return false;
    }

    // Spread samples from the first to the last item so clustered prefixes do not hide a wide range later in the input.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetSampleIndex(int sampleIndex, int sampleCount, int lastIndex) => (int)(((long)sampleIndex * lastIndex) / (sampleCount - 1));
}