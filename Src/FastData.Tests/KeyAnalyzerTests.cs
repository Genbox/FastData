using Genbox.FastData.Internal.Analysis.Data;
using Genbox.FastData.Internal.Analysis.Properties;
using static Genbox.FastData.Internal.Analysis.KeyAnalyzer;

namespace Genbox.FastData.Tests;

public class KeyAnalyzerTests
{
    [Fact]
    public void GetProperties_IsConsecutive_Test()
    {
        Assert.True(GetProperties(['a', 'b', 'c']).IsConsecutive);
        Assert.False(GetProperties(['a', 'c']).IsConsecutive);

        Assert.True(GetProperties(new sbyte[] { -1, 0, 1 }).IsConsecutive);
        Assert.False(GetProperties(new sbyte[] { -1, 1, 2 }).IsConsecutive);

        Assert.True(GetProperties(new byte[] { 1, 2, 3 }).IsConsecutive);
        Assert.False(GetProperties(new byte[] { 1, 3, 4 }).IsConsecutive);

        Assert.True(GetProperties(new short[] { 10, 11, 12 }).IsConsecutive);
        Assert.False(GetProperties(new short[] { 10, 11, 13 }).IsConsecutive);

        Assert.True(GetProperties(new ushort[] { 10, 11, 12 }).IsConsecutive);
        Assert.False(GetProperties(new ushort[] { 10, 11, 13 }).IsConsecutive);

        Assert.True(GetProperties([100, 101]).IsConsecutive);
        Assert.False(GetProperties([100, 102]).IsConsecutive);

        Assert.True(GetProperties([100u, 101u]).IsConsecutive);
        Assert.False(GetProperties([100u, 102u]).IsConsecutive);

        Assert.True(GetProperties([long.MaxValue - 2, long.MaxValue - 1, long.MaxValue]).IsConsecutive);
        Assert.False(GetProperties([1L, 3L, 4L]).IsConsecutive);

        Assert.True(GetProperties([1ul, 2ul, 3ul]).IsConsecutive);
        Assert.False(GetProperties([1ul, 2ul, 4ul]).IsConsecutive);

        Assert.True(GetProperties([0.5f, 1.5f, 2.5f]).IsConsecutive);
        Assert.False(GetProperties([0f, 0.9f, 2f]).IsConsecutive);

        Assert.True(GetProperties([0.5d, 1.5d, 2.5d]).IsConsecutive);
        Assert.False(GetProperties([0d, 0.9d, 2d]).IsConsecutive);
    }

    [Theory]
    [InlineData((object)new[] { "a", "aa", "aaa", "aaaa", "aaaaa", "aaaaaa", "aaaaaaa", "aaaaaaaa" })]
    [InlineData((object)new[] { "aaa", "aaaaa", "aaaaaa", "aaaaaaa", "aaaaaaaa", "aaaaaaaaa", "aaaaaaaaaa" })] //Test inputs that don't start with 1
    [InlineData((object)new[] { "a", "aaa", "aaaa" })] //Test when there is gaps
    [InlineData((object)new[] { "a" })] //Test when there is only one item
    [InlineData((object)new[] { "a", "a", "aaa", "aaa" })] //Test duplicates
    public void GetStringProperties_LengthMap_Test(string[] data)
    {
        StringProperties res = GetStringProperties(data);
        LengthBitArray map = res.LengthData.LengthMap;
        Assert.Equal(data.Distinct().Count(), map.Count);

        foreach (string str in data)
        {
            Assert.True(map.Get(str.Length));
        }

        Assert.Equal((uint)data.Min(x => x.Length), res.LengthData.Min);
        Assert.Equal((uint)data.Max(x => x.Length), res.LengthData.Max);
    }

    [Theory]
    [InlineData(new[] { "item1", "item2", "item3", "item4" }, 4, 0)]
    [InlineData(new[] { "1item", "2item", "3item", "4item" }, 0, 4)]
    [InlineData(new[] { "a", "aa", "aaa", "aaaaa" }, 1, 1)]
    public void GetStringProperties_EntropyData_Test(string[] data, int leftZero, int rightZero)
    {
        StringProperties res = GetStringProperties(data);
        Assert.Equal(res.DeltaData.LeftZeroCount, leftZero);
        Assert.Equal(res.DeltaData.RightZeroCount, rightZero);
    }
}