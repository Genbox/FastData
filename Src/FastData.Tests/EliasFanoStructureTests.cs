using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Tests;

public class EliasFanoStructureTests
{
    [Fact]
    public void CalculatesLargeLowerBitStorageWithoutOverflow()
    {
        Assert.Equal(33_554_433, EliasFanoStructure<long, byte>.GetLowerWordCount(56_512_728, 38));
    }

    [Fact]
    public void RejectsUInt64RangeThatCannotBeRepresentedAsInt64()
    {
        EliasFanoStructure<ulong, byte> structure = new EliasFanoStructure<ulong, byte>(0UL, 1UL << 63, 128);
        Assert.Null(structure.Create(new[] { 0UL, 1UL << 63 }, ReadOnlyMemory<byte>.Empty));
    }
}