using Genbox.FastData.Internal.Analysis;

namespace Genbox.FastData.Tests;

public class AnalyzerTests
{
    [Fact]
    public void PropertiesTest()
    {
        string[] data = ["a", "aa", "aaa", "aaaa"];
        StringProperties props = Analyzer.GetProperties(data);
        Assert.True(props.UniqLength);
        Assert.Equal(1u, props.MinStrLen);
        Assert.Equal(4u, props.MaxStrLen);
        Assert.Equal('a', props.MinChar);
        Assert.Equal('a', props.MaxChar);
    }

    [Theory]
    [InlineData("abc", 'a', 'c')]
    [InlineData("cba", 'a', 'c')] //Order shouldn't matter
    [InlineData("123456789", '1', '9')]
    [InlineData("a", 'a', 'a')] //Only one input should result in the same min/max
    public void GetCharMinMaxTest(string val, char min, char max)
    {
        (ushort minCharVal, ushort maxCharVal) = Analyzer.GetCharMinMax(val);
        Assert.Equal(min, minCharVal);
        Assert.Equal(max, maxCharVal);
    }

    [Theory]
    [InlineData(new[] { 1u, 2u, 3u }, 1, 3)]
    [InlineData(new[] { 3u, 2u, 1u }, 1, 3)] //Order shouldn't matter
    [InlineData(new[] { 0u, uint.MaxValue }, 0, uint.MaxValue)] //Check bounds
    public void GetStrMinMaxTest(uint[] lengths, uint expectedMin, uint expectedMax)
    {
        uint min = uint.MaxValue;
        uint max = 0;

        foreach (uint length in lengths)
            Analyzer.GetStrMinMax(length, ref min, ref max);

        Assert.Equal(expectedMin, min);
        Assert.Equal(expectedMax, max);
    }

    [Theory]
    [InlineData(new[] { 1u, 2u, 3u }, new[] { true, true, true })]
    [InlineData(new[] { 1u, 1u, 1u }, new[] { true, false, false })]
    [InlineData(new[] { 1u, 1u, 2u }, new[] { true, false, false })]
    public void GetStrUniqTest(uint[] lengths, bool[] expected)
    {
        bool[] lengthIndex = [false, false, false];

        bool uniqFlag = true;

        for (int i = 0; i < lengths.Length; i++)
        {
            Analyzer.GetStrUniq(lengths[i], lengthIndex, ref uniqFlag);
            Assert.Equal(expected[i], uniqFlag);
        }
    }

    [Theory]
    [InlineData(new[] { "aaa", "bbb" }, 0)]
    [InlineData(new[] { "aaa", "abb" }, 1)]
    [InlineData(new[] { "aaa", "aab" }, 2)]
    [InlineData(new[] { "aaa", "aaa" }, 3)]
    [InlineData(new[] { "aaa", "aaaa" }, 3)] //test smaller a than b
    [InlineData(new[] { "aaaa", "aaa" }, 3)] //test smaller b than a
    [InlineData(new[] { "basicincome", "basicflying", "basicmetal", "basichammer", "basicmicrowave" }, 5)]
    public void LongestLeftTest(string[] data, short expectedLength)
    {
        short val = Analyzer.LongestLeft(data);
        Assert.Equal(expectedLength, val);
    }

    [Theory]
    [InlineData(new[] { "aaa", "bbb" }, 0)]
    [InlineData(new[] { "aaa", "bba" }, 1)]
    [InlineData(new[] { "aaa", "baa" }, 2)]
    [InlineData(new[] { "aaa", "aaa" }, 3)]
    [InlineData(new[] { "aaa", "aaaa" }, 3)] //test smaller a than b
    [InlineData(new[] { "aaaa", "aaa" }, 3)] //test smaller b than a
    [InlineData(new[] { "incomebasic", "flyingbasic", "waterbasic", "metalbasic", "hammerbasic", "microwavebasic" }, 5)]
    public void LongestRightTest(string[] data, short expectedLength)
    {
        short val = Analyzer.LongestRight(data);
        Assert.Equal(expectedLength, val);
    }
}