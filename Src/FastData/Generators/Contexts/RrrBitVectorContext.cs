using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

[SuppressMessage("Major Code Smell", "S2326:Unused type parameters should be removed")]
public sealed class RrrBitVectorContext<TKey, TValue>(ulong minValue, ulong maxValue, int blockSize, byte[] classes, uint[] offsets) : IContext<TValue>
{
    public ulong MinValue { get; } = minValue;
    public ulong MaxValue { get; } = maxValue;
    public int BlockSize { get; } = blockSize;
    public byte[] Classes { get; } = classes;
    public uint[] Offsets { get; } = offsets;
    public ReadOnlyMemory<TValue> Values { get; } = ReadOnlyMemory<TValue>.Empty;
}
