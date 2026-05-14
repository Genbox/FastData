using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Helpers;

namespace Genbox.FastData.Generator.Tests;

public class FormatHelperTests
{
    [Fact]
    public void FormatColumnsReturnsEmptyForNoItems()
    {
        int[] items = [];
        string result = FormatHelper.FormatColumns(items, x => x.ToStringInvariant(), 2, 2);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void FormatColumnsSplitsByColumnCount()
    {
        int[] items = [1, 2, 3, 4];
        string result = FormatHelper.FormatColumns(items, x => x.ToStringInvariant(), 2, 2);

        string expected = $"  1, 2, {Environment.NewLine}  3, 4";
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatListJoinsWithDelimiter()
    {
        string[] items = ["a", "b", "c"];
        string result = FormatHelper.FormatList(items, x => x, " | ");

        Assert.Equal("a | b | c", result);
    }
}