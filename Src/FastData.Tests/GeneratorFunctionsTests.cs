using Genbox.FastData.Generators;

namespace Genbox.FastData.Tests;

public class GeneratorFunctionsTests
{
    [Theory]
    [InlineData("abc", 0, 'a')]
    [InlineData("abc", -1, 'c')]
    public void UnitAt_ReturnsUnitWhenOffsetIsValid(string value, int offset, char expected)
    {
        Assert.Equal(expected, GeneratorFunctions.UnitAt(value, offset));
    }

    [Theory]
    [InlineData("prefix", 0, "pre")]
    [InlineData("prefix", -3, "fix")]
    public void EqualsAt_ReturnsTrueWhenFragmentFitsAtOffset(string value, int offset, string fragment)
    {
        Assert.True(GeneratorFunctions.EqualsAt(value, offset, fragment));
    }

    [Theory]
    [InlineData("prefix", 0, "PRE")]
    [InlineData("prefix", -3, "FIX")]
    public void EqualsAtAsciiLower_ReturnsTrueWhenFragmentFitsAtOffset(string value, int offset, string fragment)
    {
        Assert.True(GeneratorFunctions.EqualsAtAsciiLower(value, offset, fragment));
    }

    [Theory]
    [InlineData("", true)]
    [InlineData("abc", true)]
    [InlineData("abcd", true)]
    [InlineData("abcdefg", true)]
    [InlineData("abc\u007f", true)]
    [InlineData("abc\u0080", false)]
    [InlineData("\u00e9", false)]
    [InlineData("abc\u00e9", false)]
    public void IsAsciiOnly_ReturnsExpectedResult(string value, bool expected)
    {
        Assert.Equal(expected, GeneratorFunctions.IsAsciiOnly(value));
    }
}