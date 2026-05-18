using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Tests;

public class RrrBitVectorStructureTests
{
    [Fact]
    public void RejectsFullUInt64Universe()
    {
        RrrBitVectorStructure<ulong, byte> structure = new RrrBitVectorStructure<ulong, byte>(ulong.MinValue, ulong.MaxValue, true);
        Assert.Null(structure.Create(new[] { ulong.MinValue, ulong.MaxValue }, ReadOnlyMemory<byte>.Empty));
    }
}