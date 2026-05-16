using Genbox.FastData.Internal.Pgm;
using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Tests;

public class PgmTests
{
    [Fact]
    public void PgmStructure_RejectsSentinelKeyAfterSorting()
    {
        PgmStructure<int, byte> structure = new PgmStructure<int, byte>(false);

        Assert.Throws<ArgumentException>(() => structure.Create(new[] { 3, int.MaxValue, 1 }, ReadOnlyMemory<byte>.Empty));
    }

    [Fact]
    public void PgmBuilder_FindsDuplicateInt32Keys()
    {
        int[] keys = [1, 1, 2, 4, 4, 10, 100];

        Assert.True(Contains(keys, 4, 1));
        Assert.True(Contains(keys, 4, 4));
        Assert.True(Contains(keys, 4, 100));
        Assert.False(Contains(keys, 4, 3));
    }

    [Fact]
    public void PgmBuilder_FindsUnsignedLargeGapKeys()
    {
        ulong[] keys = [0ul, 1ul, 4_294_967_296ul, 9_223_372_036_854_775_808ul, ulong.MaxValue - 1];

        Assert.True(Contains(keys, 8, 0ul));
        Assert.True(Contains(keys, 8, 4_294_967_296ul));
        Assert.True(Contains(keys, 8, ulong.MaxValue - 1));
        Assert.False(Contains(keys, 8, 2ul));
    }

    [Fact]
    public void PgmBuilder_FindsInt64LargeGapKeys()
    {
        long[] keys = [long.MinValue, -10_000_000_000L, -1L, 0L, 10_000_000_000L, long.MaxValue - 1];

        Assert.True(Contains(keys, 8, long.MinValue));
        Assert.True(Contains(keys, 8, 0L));
        Assert.True(Contains(keys, 8, long.MaxValue - 1));
        Assert.False(Contains(keys, 8, 1L));
    }

    [Fact]
    public void PgmBuilder_FindsFloatingPointKeys()
    {
        float[] keys = [-10.5f, -1.25f, 0f, 0.5f, 100.25f];

        Assert.True(Contains(keys, 4, -10.5f));
        Assert.True(Contains(keys, 4, 0f));
        Assert.True(Contains(keys, 4, 100.25f));
        Assert.False(Contains(keys, 4, 1.5f));
    }

    private static bool Contains<T>(T[] keys, int epsilon, T key) where T : notnull
    {
        PgmSegment<T>[] segments = PgmBuilder<T>.Build(keys, epsilon);

        int seg = 0;
        int segHi = segments.Length - 1;
        while (seg < segHi)
        {
            int mid = seg + ((segHi - seg) >> 1);
            if (Comparer<T>.Default.Compare(segments[mid + 1].Key, key) <= 0)
                seg = mid + 1;
            else
                segHi = mid;
        }

        int pos = segments[seg].Evaluate(key);
        int lo = Math.Max(0, pos - epsilon);
        int hi = Math.Min(keys.Length, pos + epsilon + 2);
        return Array.BinarySearch(keys, lo, hi - lo, key, Comparer<T>.Default) >= 0;
    }
}