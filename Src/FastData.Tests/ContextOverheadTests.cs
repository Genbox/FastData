using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Generators.Contexts.Misc;
using Genbox.FastData.Internal.Pgm;

namespace Genbox.FastData.Tests;

public class ContextOverheadTests
{
    [Fact]
    public void KeyAndValueOnlyContextsHaveNoOverhead()
    {
        int[] keys = [1, 2];
        string[] values = ["one", "two"];
        IContext[] contexts =
        [
            new ArrayContext<int, string>(keys, values),
            new BinarySearchContext<int, string>(keys, values),
            new BinarySearchInterpolationContext<int, string>(keys, values),
            new ConditionalContext<int, string>(keys, values),
            new RangeContext<int>(new (int Start, int End)[] { (1, 2) }),
            new SingleValueContext<int, string>(1, values.AsMemory(0, 1)),
            new EarlyExitOnlyContext()
        ];

        Assert.All(contexts, static context => Assert.Equal(0, context.GetOverheadBytes()));
    }

    [Fact]
    public void BitSetsCountPersistedWordsOnly()
    {
        BitSetContext<string> bitSet = new BitSetContext<string>(new ulong[2], new string[100]);
        BitSetContext<string> denseValues = new BitSetContext<string>([], new string[100], false);
        BloomFilterContext bloomFilter = new BloomFilterContext(new ulong[3]);

        Assert.Equal(16, bitSet.GetOverheadBytes());
        Assert.Equal(0, denseValues.GetOverheadBytes());
        Assert.Equal(24, bloomFilter.GetOverheadBytes());
    }

    [Fact]
    public void HashTableCountsBucketsNextIndicesAndOptionalHashes()
    {
        int[] buckets = new int[2];
        HashTableEntry<int>[] entries = new HashTableEntry<int>[3];
        HashTableContext<int, string> withoutHashes = new HashTableContext<int, string>(buckets, entries, false, new string[3]);
        HashTableContext<int, string> withHashes = new HashTableContext<int, string>(buckets, entries, true, new string[3]);

        Assert.Equal(20, withoutHashes.GetOverheadBytes());
        Assert.Equal(44, withHashes.GetOverheadBytes());
    }

    [Fact]
    public void CompactHashTableCountsBucketStartsAndOptionalHashes()
    {
        int[] bucketStarts = new int[3];
        HashTableCompactEntry<int>[] entries = new HashTableCompactEntry<int>[2];
        HashTableCompactContext<int, string> withoutHashes = new HashTableCompactContext<int, string>(bucketStarts, entries, false, new string[2]);
        HashTableCompactContext<int, string> withHashes = new HashTableCompactContext<int, string>(bucketStarts, entries, true, new string[2]);

        Assert.Equal(12, withoutHashes.GetOverheadBytes());
        Assert.Equal(28, withHashes.GetOverheadBytes());
    }

    [Fact]
    public void PerfectHashTableCountsOptionalHashesOnly()
    {
        KeyValuePair<int, ulong>[] data = new KeyValuePair<int, ulong>[2];
        HashTablePerfectContext<int, string> withoutHashes = new HashTablePerfectContext<int, string>(data, false, new string[2]);
        HashTablePerfectContext<int, string> withHashes = new HashTablePerfectContext<int, string>(data, true, new string[2]);

        Assert.Equal(0, withoutHashes.GetOverheadBytes());
        Assert.Equal(16, withHashes.GetOverheadBytes());
    }

    [Fact]
    public void IndexedContextsCountIndexMetadataOnly()
    {
        HybleContext<int, string> hyble = new HybleContext<int, string>(new KeyValuePair<int, ulong>[5], new ushort[3], 5, 3, 1, new string[5]);
        ConstMapContext<int, string> constMap = new ConstMapContext<int, string>(new int[5], new string[5], new uint[7], 1, 4, 4);
        KeyLengthContext<string> keyLength = new KeyLengthContext<string>(new string?[10], 1, new string[2], new int[4]);
        KeyLengthContext<string> keyLengthWithoutValues = new KeyLengthContext<string>(new string?[10], 1, ReadOnlyMemory<string>.Empty, []);

        Assert.Equal(6, hyble.GetOverheadBytes());
        Assert.Equal(28, constMap.GetOverheadBytes());
        Assert.Equal(16, keyLength.GetOverheadBytes());
        Assert.Equal(0, keyLengthWithoutValues.GetOverheadBytes());
    }

    [Fact]
    public void EncodedStructuresCountPersistedBuffers()
    {
        EliasFanoContext eliasFano = new EliasFanoContext(3, 7, new ulong[2], new ulong[3], 100, 7, new int[4], 0, 100);
        EliasFanoContext eliasFanoWithoutLowerBits = new EliasFanoContext(0, 0, new ulong[2], [], 100, 7, new int[4], 0, 100);
        RrrBitVectorContext rrr = new RrrBitVectorContext(0, 100, 15, new byte[3], new uint[2]);

        Assert.Equal(56, eliasFano.GetOverheadBytes());
        Assert.Equal(32, eliasFanoWithoutLowerBits.GetOverheadBytes());
        Assert.Equal(11, rrr.GetOverheadBytes());
    }

    [Fact]
    public void PgmCountsModelMetadataOnlyWhenSegmentArraysArePersisted()
    {
        PgmSegment<int>[] segments =
        [
            new PgmSegment<int>(1, 1, 0),
            new PgmSegment<int>(5, 0.5f, 4),
            new PgmSegment<int>(int.MaxValue, 0, 10)
        ];
        PgmContext<int, string> singleSegment = new PgmContext<int, string>(new int[2], new string[2], segments, [0, 2], 64, 4, 1);
        PgmContext<int, string> multipleSegments = new PgmContext<int, string>(new int[2], new string[2], segments, [0, 2], 64, 4, 2);

        Assert.Equal(0, singleSegment.GetOverheadBytes());
        Assert.Equal(24, multipleSegments.GetOverheadBytes());
    }
}