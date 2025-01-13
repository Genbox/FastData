using Genbox.FastData.Internal.Analysis;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Tests;

public class DataAnalyzerTests
{
    [Fact]
    public void GetStringProperties_LengthMap_Test()
    {
        object[] data = ["a", "aa", "aaa", "azza"];
        StringProperties res = DataAnalyzer.GetStringProperties(data);
        IntegerBitSet map = res.LengthData.LengthMap;

        Assert.Equal(4u, map.Count);
        Assert.Equal(1u, map.MinValue);
        Assert.Equal(4u, map.MaxValue);

        //Anything over 64 should fail (at least for now)
        Assert.False(map.Contains(100));
    }

    [Theory]
    [InlineData(new[] { "item1", "item2", "item3", "item4" }, true, 4)]
    [InlineData(new[] { "1item", "2item", "3item", "4item" }, false, 4)]
    [InlineData(new[] { "a", "aa", "aaa", "aaaaa" }, true, 1)]
    public void GetStringProperties_EntropyData_Test(object[] data, bool left, int length)
    {
        StringProperties res = DataAnalyzer.GetStringProperties(data);
        (bool Left, _, int Length) = res.EntropyData.GetJustify();
        Assert.Equal(left, Left);
        Assert.Equal(length, Length);
    }

    [Theory]
    [InlineData(new[] { "item1", "item2", "item3", "item4" }, '1', 't')]
    public void GetStringProperties_CharacterMap_Test(object[] data, char minChar, char maxChar)
    {
        StringProperties res = DataAnalyzer.GetStringProperties(data);
        CharacterMap map = res.CharacterData.CharacterMap;

        Assert.True(map.Contains(minChar));
        Assert.True(map.Contains(maxChar));

        Assert.Equal(minChar, map.MinChar);
        Assert.Equal(maxChar, map.MaxChar);
    }
}