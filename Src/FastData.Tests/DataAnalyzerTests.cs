using Genbox.FastData.Internal.Analysis.Data;
using Genbox.FastData.Internal.Analysis.Misc;
using Genbox.FastData.Internal.Analysis.Properties;
using static Genbox.FastData.Internal.Analysis.DataAnalyzer;

namespace Genbox.FastData.Tests;

public class DataAnalyzerTests
{
    [Theory]
    [InlineData((object)new[] { "a", "aa", "aaa", "aaaa", "aaaaa", "aaaaaa", "aaaaaaa", "aaaaaaaa" })]
    [InlineData((object)new[] { "aaa", "aaaaa", "aaaaaa", "aaaaaaa", "aaaaaaaa", "aaaaaaaaa", "aaaaaaaaaa" })] //Test inputs that don't start with 1
    [InlineData((object)new[] { "a", "aaa", "aaaa" })] //Test when there is gaps
    [InlineData((object)new[] { "a" })] //Test when there is only one item
    [InlineData((object)new[] { "a", "a", "aaa", "aaa" })] //Test duplicates
    public void GetStringProperties_LengthMap_Test(string[] data)
    {
        StringProperties res = GetStringProperties(data);
        IntegerBitSet map = res.LengthData.LengthMap;
        Assert.Equal((uint)data.Distinct().Count(), map.Count);

        foreach (string str in data)
        {
            Assert.True(map.Contains(str.Length));
        }

        Assert.Equal((uint)data.Min(x => x.Length), map.MinValue);
        Assert.Equal((uint)data.Max(x => x.Length), map.MaxValue);

        //Anything over 64 should fail (at least for now)
        Assert.False(map.Contains(100));
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