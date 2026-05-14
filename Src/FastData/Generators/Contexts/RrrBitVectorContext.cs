using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides RRR-compressed bit-vector data for generated structures.</summary>
public sealed class RrrBitVectorContext(ulong minValue, ulong maxValue, int blockSize, byte[] classes, uint[] offsets) : IContext
{
    /// <summary>Gets the minimum original value represented by the bit vector.</summary>
    public ulong MinValue { get; } = minValue;
    /// <summary>Gets the maximum original value represented by the bit vector.</summary>
    public ulong MaxValue { get; } = maxValue;
    /// <summary>Gets the number of bits per compressed block.</summary>
    public int BlockSize { get; } = blockSize;
    /// <summary>Gets the popcount class for each compressed block.</summary>
    public byte[] Classes { get; } = classes;
    /// <summary>Gets the offset table for each compressed block.</summary>
    public uint[] Offsets { get; } = offsets;
}