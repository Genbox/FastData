using Genbox.FastData.Config;
using Genbox.FastData.Internal;

namespace Genbox.FastData.Tests;

public class DeduplicationTests
{
    [Fact]
    public void DeduplicateKeys_HashSet_RemovesDuplicatesPreservesFirst()
    {
        ReadOnlyMemory<int> keys = (int[])[3, 1, 3, 2];
        ReadOnlyMemory<int> values = (int[])[30, 10, 31, 20];
        NumericDataConfig cfg = new NumericDataConfig
        {
            DeduplicationMode = DeduplicationMode.HashSet,
            ThrowOnDuplicates = false
        };

        bool isSorted = Deduplication.DeduplicateKeys(cfg, ref keys, ref values, EqualityComparer<int>.Default, Comparer<int>.Default);

        Assert.False(isSorted);
        Assert.Equal(new[] { 3, 1, 2 }, keys);
        Assert.Equal(new[] { 30, 10, 20 }, values);
    }

    [Fact]
    public void DeduplicateKeys_HashSet_ThrowsOnDuplicates()
    {
        ReadOnlyMemory<int> keys = (int[])[1, 1];
        ReadOnlyMemory<int> values = (int[])[10, 20];
        NumericDataConfig cfg = new NumericDataConfig
        {
            DeduplicationMode = DeduplicationMode.HashSet,
            ThrowOnDuplicates = true
        };

        Assert.Throws<InvalidOperationException>(() => Deduplication.DeduplicateKeys(cfg, ref keys, ref values, EqualityComparer<int>.Default, Comparer<int>.Default));
    }

    [Fact]
    public void DeduplicateKeys_SortPreserveOrder_KeepsInputOrder()
    {
        ReadOnlyMemory<int> keys = (int[])[3, 1, 3, 2];
        ReadOnlyMemory<int> values = (int[])[30, 10, 30, 20];
        NumericDataConfig cfg = new NumericDataConfig
        {
            DeduplicationMode = DeduplicationMode.Sort,
            PreserveOrder = true,
            ThrowOnDuplicates = false
        };

        bool isSorted = Deduplication.DeduplicateKeys(cfg, ref keys, ref values, EqualityComparer<int>.Default, Comparer<int>.Default);

        Assert.False(isSorted);
        Assert.Equal(new[] { 3, 1, 2 }, keys);
        Assert.Equal(new[] { 30, 10, 20 }, values);
    }

    [Fact]
    public void DeduplicateKeys_SortWithoutPreserveOrder_ReturnsSorted()
    {
        ReadOnlyMemory<int> keys = (int[])[3, 1, 3, 2];
        ReadOnlyMemory<int> values = (int[])[30, 10, 30, 20];
        NumericDataConfig cfg = new NumericDataConfig
        {
            DeduplicationMode = DeduplicationMode.Sort,
            PreserveOrder = false,
            ThrowOnDuplicates = false
        };

        bool isSorted = Deduplication.DeduplicateKeys(cfg, ref keys, ref values, EqualityComparer<int>.Default, Comparer<int>.Default);

        Assert.True(isSorted);
        Assert.Equal(new[] { 1, 2, 3 }, keys);
        Assert.Equal(new[] { 10, 20, 30 }, values);
    }

    [Fact]
    public void DeduplicateKeys_Disabled_DoesNotModify()
    {
        ReadOnlyMemory<int> keys = (int[])[2, 2, 1];
        ReadOnlyMemory<int> values = (int[])[20, 21, 10];
        NumericDataConfig cfg = new NumericDataConfig
        {
            DeduplicationMode = DeduplicationMode.Disabled
        };

        bool isSorted = Deduplication.DeduplicateKeys(cfg, ref keys, ref values, EqualityComparer<int>.Default, Comparer<int>.Default);

        Assert.False(isSorted);
        Assert.Equal(new[] { 2, 2, 1 }, keys);
        Assert.Equal(new[] { 20, 21, 10 }, values);
    }
}