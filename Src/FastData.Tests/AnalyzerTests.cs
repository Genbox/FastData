using Genbox.FastData.Internal.Analysis;

namespace Genbox.FastData.Tests;

public class AnalyzerTests
{
    [Fact]
    public void GetStringProperties_LengthMap_Test()
    {
        object[] data = ["a", "aa", "aaa", "azza"];
        StringProperties res = Analyzer.GetStringProperties(data);

        Assert.Equal(4u, res.LengthMap.Count);
        Assert.Equal(1u, res.LengthMap.MinValue);
        Assert.Equal(4u, res.LengthMap.MaxValue);

        //Anything over 64 should fail (at least for now)
        Assert.False(res.LengthMap.Contains(100));
    }

    [Theory]
    [InlineData(new[] { "item1", "item2", "item3", "item4" }, true, 4)]
    [InlineData(new[] { "1item", "2item", "3item", "4item" }, false, 4)]
    [InlineData(new[] { "a", "aa", "aaa", "aaaaa" }, true, 1)]
    public void GetStringProperties_EntropyData_Test(object[] data, bool left, int length)
    {
        StringProperties res = Analyzer.GetStringProperties(data);
        (bool Left, int Length) = res.EntropyData.GetJustify();
        Assert.Equal(left, Left);
        Assert.Equal(length, Length);
    }

    [Theory]
    [InlineData(new[] { "item1", "item2", "item3", "item4" }, '1', 't')]
    public void GetStringProperties_CharacterMap_Test(object[] data, char minChar, char maxChar)
    {
        StringProperties res = Analyzer.GetStringProperties(data);
        Assert.True(res.CharacterMap.Contains(minChar));
        Assert.True(res.CharacterMap.Contains(maxChar));

        Assert.Equal(minChar, res.CharacterMap.MinChar);
        Assert.Equal(maxChar, res.CharacterMap.MaxChar);
    }
}