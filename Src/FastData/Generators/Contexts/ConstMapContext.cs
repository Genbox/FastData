using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for exact binary-fuse candidate maps.</summary>
public sealed class ConstMapContext<TKey, TValue>(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values, uint[] data, ulong seed, uint segmentLength, uint segmentCountLength) : ConstMapContext(data, seed, segmentLength, segmentCountLength)
{
    /// <summary>Gets the compact keys indexed by the candidate ordinal recovered from <see cref="ConstMapContext.Data" />.</summary>
    public ReadOnlyMemory<TKey> Keys { get; } = keys;

    /// <summary>Gets the values aligned with <see cref="Keys" />.</summary>
    public ReadOnlyMemory<TValue> Values { get; } = values;
}

/// <summary>Provides metadata shared by generated ConstMap structures.</summary>
public abstract class ConstMapContext(uint[] data, ulong seed, uint segmentLength, uint segmentCountLength) : IContext
{
    /// <summary>Gets the binary-fuse cells whose XOR recovers a candidate key ordinal.</summary>
    public uint[] Data { get; } = data;

    /// <summary>Gets the seed used to mix the generated base hash.</summary>
    public ulong Seed { get; } = seed;

    /// <summary>Gets the power-of-two segment length.</summary>
    public uint SegmentLength { get; } = segmentLength;

    /// <summary>Gets the length covered by the first segment range.</summary>
    public uint SegmentCountLength { get; } = segmentCountLength;

    /// <inheritdoc />
    public long GetOverheadBytes() => Data.LongLength * sizeof(uint);
}