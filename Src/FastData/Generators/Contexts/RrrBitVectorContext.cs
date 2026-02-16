using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

public class RrrBitVectorContext(ulong minValue, ulong maxValue, int blockSize, byte[] classes, uint[] offsets) : IContext
{
    public ulong MinValue { get; } = minValue;
    public ulong MaxValue { get; } = maxValue;
    public int BlockSize { get; } = blockSize;
    public byte[] Classes { get; } = classes;
    public uint[] Offsets { get; } = offsets;
}