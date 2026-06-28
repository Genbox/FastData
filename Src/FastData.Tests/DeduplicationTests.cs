using Genbox.FastData.Internal;

namespace Genbox.FastData.Tests;

public class DeduplicationTests
{
    [Fact]
    public void DeduplicateWithSort_ByteKeys_SortsAndCompacts()
    {
        byte[] keys = Enumerable.Range(0, 128).Select(static value => (byte)(127 - value % 64)).ToArray();

        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);

        Assert.Equal(64, uniqueCount);
        Assert.Equal(Enumerable.Range(64, 64).Select(static value => (byte)value), keys.AsSpan(0, uniqueCount).ToArray());
    }

    [Fact]
    public void DeduplicateWithSort_UInt16Keys_SortsAndCompacts()
    {
        ushort[] keys = Enumerable.Range(0, 128).Select(static value => (ushort)(300 - value % 64)).ToArray();

        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);

        Assert.Equal(64, uniqueCount);
        Assert.Equal(Enumerable.Range(237, 64).Select(static value => (ushort)value), keys.AsSpan(0, uniqueCount).ToArray());
    }

    [Fact]
    public void DeduplicateWithSort_IntKeys_SortsAndKeepsValuesAligned()
    {
        int[] keys = Enumerable.Range(0, 100).Select(static value => 50 - value).ToArray();
        int[] values = keys.Select(static key => key * 10).ToArray();

        Deduplication.DeduplicateNumericKeysInternal(keys, values, out int uniqueCount);

        Assert.Equal(100, uniqueCount);

        for (int i = 0; i < uniqueCount; i++)
        {
            Assert.Equal(-49 + i, keys[i]);
            Assert.Equal(keys[i] * 10, values[i]);
        }
    }

    [Fact]
    public void DeduplicateWithSort_LongKeys_SortsSignedValues()
    {
        long[] keys = [long.MaxValue, 0, -1, long.MinValue, 42, -42, 42, 0, long.MaxValue, 17, -100, 99, 5, -5, 6, -6, 7, -7, 8, -8, 9, -9, 10, -10, 11, -11, 12, -12, 13, -13, 14, -14, 15, -15, 16, -16, 18, -18, 19, -19];

        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);

        long[] uniqueKeys = keys.AsSpan(0, uniqueCount).ToArray();
        Assert.Equal(uniqueKeys.OrderBy(static key => key), uniqueKeys);
        Assert.Equal(uniqueKeys.Distinct(), uniqueKeys);
    }

    [Fact]
    public void DeduplicateWithSort_IntegerBoundaryKeys_FallBackAndSort()
    {
        short[] shortKeys = [short.MaxValue, 0, short.MinValue];
        ushort[] ushortKeys = [ushort.MaxValue, 0, 42];
        int[] intKeys = [int.MaxValue, 0, int.MinValue];
        long[] longKeys = [long.MaxValue, 0, long.MinValue];
        uint[] uintKeys = [uint.MaxValue, 0, 42];
        ulong[] ulongKeys = [ulong.MaxValue, 0, 42];

        Deduplication.DeduplicateNumericKeysInternal(shortKeys, Array.Empty<int>(), out int shortCount);
        Deduplication.DeduplicateNumericKeysInternal(ushortKeys, Array.Empty<int>(), out int ushortCount);
        Deduplication.DeduplicateNumericKeysInternal(intKeys, Array.Empty<int>(), out int intCount);
        Deduplication.DeduplicateNumericKeysInternal(longKeys, Array.Empty<int>(), out int longCount);
        Deduplication.DeduplicateNumericKeysInternal(uintKeys, Array.Empty<int>(), out int uintCount);
        Deduplication.DeduplicateNumericKeysInternal(ulongKeys, Array.Empty<int>(), out int ulongCount);

        Assert.Equal(3, shortCount);
        Assert.Equal([short.MinValue, 0, short.MaxValue], shortKeys.AsSpan(0, shortCount).ToArray());
        Assert.Equal(3, ushortCount);
        Assert.Equal([0, 42, ushort.MaxValue], ushortKeys.AsSpan(0, ushortCount).ToArray());
        Assert.Equal(3, intCount);
        Assert.Equal([int.MinValue, 0, int.MaxValue], intKeys.AsSpan(0, intCount).ToArray());
        Assert.Equal(3, longCount);
        Assert.Equal([long.MinValue, 0, long.MaxValue], longKeys.AsSpan(0, longCount).ToArray());
        Assert.Equal(3, uintCount);
        Assert.Equal([0u, 42u, uint.MaxValue], uintKeys.AsSpan(0, uintCount).ToArray());
        Assert.Equal(3, ulongCount);
        Assert.Equal([0ul, 42ul, ulong.MaxValue], ulongKeys.AsSpan(0, ulongCount).ToArray());
    }
}