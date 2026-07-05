using Genbox.FastData.Config;
using Genbox.FastData.Internal;

namespace Genbox.FastData.Tests;

public class DeduplicationTests
{
    [Fact]
    public void DeduplicateNumericKeys_UInt8Keys_SortsAndCompacts()
    {
        byte[] keys = Enumerable.Range(0, 128).Select(static value => (byte)(127 - value % 64)).ToArray();

        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);

        Assert.Equal(64, uniqueCount);
        Assert.Equal(Enumerable.Range(64, 64).Select(static value => (byte)value), keys.AsSpan(0, uniqueCount).ToArray());
    }

    [Fact]
    public void DeduplicateNumericKeys_UInt16Keys_SortsAndCompacts()
    {
        ushort[] keys = Enumerable.Range(0, 128).Select(static value => (ushort)(300 - value % 64)).ToArray();

        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);

        Assert.Equal(64, uniqueCount);
        Assert.Equal(Enumerable.Range(237, 64).Select(static value => (ushort)value), keys.AsSpan(0, uniqueCount).ToArray());
    }

    [Fact]
    public void DeduplicateNumericKeys_Int32Keys_SortsAndKeepsValuesAligned()
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
    public void DeduplicateNumericKeys_Int64Keys_SortsSignedValues()
    {
        long[] keys = [long.MaxValue, 0, -1, long.MinValue, 42, -42, 42, 0, long.MaxValue, 17, -100, 99, 5, -5, 6, -6, 7, -7, 8, -8, 9, -9, 10, -10, 11, -11, 12, -12, 13, -13, 14, -14, 15, -15, 16, -16, 18, -18, 19, -19];

        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);

        long[] uniqueKeys = keys.AsSpan(0, uniqueCount).ToArray();
        Assert.Equal(uniqueKeys.OrderBy(static key => key), uniqueKeys);
        Assert.Equal(uniqueKeys.Distinct(), uniqueKeys);
    }

    [Fact]
    public void DeduplicateNumericKeys_ThrowOnDuplicates_Throws()
    {
        NumericDataConfig cfg = new NumericDataConfig { ThrowOnDuplicates = true };
        ReadOnlyMemory<int> keys = new[] { 1, 2, 3, 1 };
        ReadOnlyMemory<int> values = ReadOnlyMemory<int>.Empty;

        Assert.Throws<InvalidOperationException>(() => Deduplication.DeduplicateNumericKeys(cfg, ref keys, ref values));
    }

    [Fact]
    public void DeduplicateStringKeys_SortsAndCompacts()
    {
        string[] keys = ["banana", "apple", "cherry", "banana", "apple", "date"];
        string[] values = ["b1", "a1", "c1", "b2", "a2", "d1"];

        Deduplication.DeduplicateStringKeysInternal(keys, values, StringComparer.Ordinal, StringComparer.Ordinal, out int uniqueCount);

        Assert.Equal(4, uniqueCount);

        string[] uniqueKeys = keys.AsSpan(0, uniqueCount).ToArray();
        Assert.Equal(uniqueKeys.OrderBy(static key => key, StringComparer.Ordinal), uniqueKeys);
        Assert.Equal(uniqueKeys.Distinct(), uniqueKeys);
    }

    [Fact]
    public void DeduplicateStringKeys_CaseInsensitive_SortsAndCompacts()
    {
        string[] keys = ["Banana", "apple", "BANANA", "Apple"];

        Deduplication.DeduplicateStringKeysInternal(keys, Array.Empty<int>(), StringComparer.OrdinalIgnoreCase, StringComparer.OrdinalIgnoreCase, out int uniqueCount);

        Assert.Equal(2, uniqueCount);
    }

    [Fact]
    public void DeduplicateStringKeys_ThrowOnDuplicates_Throws()
    {
        StringDataConfig cfg = new StringDataConfig { ThrowOnDuplicates = true };
        ReadOnlyMemory<string> keys = new[] { "a", "b", "a" };
        ReadOnlyMemory<int> values = ReadOnlyMemory<int>.Empty;

        Assert.Throws<InvalidOperationException>(() => Deduplication.DeduplicateStringKeys(cfg, ref keys, ref values, StringComparer.Ordinal, StringComparer.Ordinal));
    }

    [Fact]
    public void DeduplicateNumericKeys_ShrinkCompaction_ProducesCorrectSlice()
    {
        // Create an array where less than half are unique to trigger ShouldShrink
        int[] raw = new int[100];

        for (int i = 0; i < 100; i++)
            raw[i] = i % 10;

        NumericDataConfig cfg = new NumericDataConfig { ThrowOnDuplicates = false };
        ReadOnlyMemory<int> keys = raw;
        ReadOnlyMemory<int> values = ReadOnlyMemory<int>.Empty;

        Deduplication.DeduplicateNumericKeys(cfg, ref keys, ref values);

        Assert.Equal(10, keys.Length);
        int[] uniqueKeys = keys.ToArray();
        Assert.Equal(uniqueKeys.OrderBy(static key => key), uniqueKeys);
        Assert.Equal(uniqueKeys.Distinct(), uniqueKeys);
    }
}