using Genbox.FastData.Internal.Enums;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Tests;

public class SegmentHelperTests
{
    [Theory]
    [InlineData((int)Alignment.Left, 2u, 3, 2, 5)]
    [InlineData((int)Alignment.Right, 1u, 2, 7, 9)]
    public void ConvertToOffsets_ReturnsExpected(int alignmentValue, uint offset, int length, int expectedStart, int expectedEnd)
    {
        Alignment alignment = (Alignment)alignmentValue;
        ArraySegment segment = new ArraySegment(offset, length, alignment);

        SegmentHelper.ConvertToOffsets(10, in segment, out int start, out int end);

        Assert.Equal(expectedStart, start);
        Assert.Equal(expectedEnd, end);
    }

    [Theory]
    [InlineData((int)Alignment.Left, 2u, 2, 8)]
    [InlineData((int)Alignment.Right, 2u, 0, 6)]
    public void ConvertToOffsets_LengthMinusOneUsesRemainder(int alignmentValue, uint offset, int expectedStart, int expectedEnd)
    {
        Alignment alignment = (Alignment)alignmentValue;
        ArraySegment segment = new ArraySegment(offset, -1, alignment);

        SegmentHelper.ConvertToOffsets(8, in segment, out int start, out int end);

        Assert.Equal(expectedStart, start);
        Assert.Equal(expectedEnd, end);
    }

    [Fact]
    public void InsertSegmentBounds_WrapsSegment()
    {
        ArraySegment segment = new ArraySegment(1, 3, Alignment.Left);
        string result = SegmentHelper.InsertSegmentBounds("abcdef", in segment);

        Assert.Equal("a[bcd]ef", result);
    }

    [Fact]
    public void GetSpan_ReturnsSegmentSpan()
    {
        ArraySegment segment = new ArraySegment(1, 2, Alignment.Right);
        string result = SegmentHelper.GetSpan("abcdef", in segment).ToString();

        Assert.Equal("de", result);
    }
}