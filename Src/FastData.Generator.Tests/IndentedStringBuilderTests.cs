namespace Genbox.FastData.Generator.Tests;

public class IndentedStringBuilderTests
{
    [Fact]
    public void AppendLineAppliesIndentAfterNewline()
    {
        IndentedStringBuilder builder = new IndentedStringBuilder { Indent = 2 };
        builder.Append("a");
        builder.AppendLine("b");
        builder.Append("c");

        string expected = $"  ab{Environment.NewLine}  c";
        Assert.Equal(expected, builder.ToString());
    }

    [Fact]
    public void AppendLineWithoutContentDoesNotIndentEmptyLine()
    {
        IndentedStringBuilder builder = new IndentedStringBuilder { Indent = 4 };
        builder.AppendLine();
        builder.Append("x");

        string expected = $"{Environment.NewLine}    x";
        Assert.Equal(expected, builder.ToString());
    }

    [Fact]
    public void DecrementIndentStopsAtZero()
    {
        IndentedStringBuilder builder = new IndentedStringBuilder { Indent = 1 };
        builder.DecrementIndent();
        builder.DecrementIndent();
        builder.Append("x");

        Assert.Equal("x", builder.ToString());
    }
}