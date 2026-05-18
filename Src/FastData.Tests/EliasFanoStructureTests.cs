using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Tests;

public class EliasFanoStructureTests
{
    [Fact]
    public void RejectsUInt64RangeThatCannotBeRepresentedAsInt64()
    {
        EliasFanoStructure<ulong, byte> structure = new EliasFanoStructure<ulong, byte>(0UL, 1UL << 63, true, 128);
        Assert.Null(structure.Create(new[] { 0UL, 1UL << 63 }, ReadOnlyMemory<byte>.Empty));
    }
}