using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

public sealed class EliasFanoContext<TKey, TValue>(ReadOnlyMemory<TKey> data, int lowerBitCount, ulong lowerMask, ulong[] upperBits, ulong[] lowerBits, int upperBitLength, int sampleRateShift, int[] samplePositions) : IContext<TValue>
{
    public ReadOnlyMemory<TKey> Data { get; } = data;
    public ReadOnlyMemory<TValue> Values { get; } = Array.Empty<TValue>();
    public int LowerBitCount { get; } = lowerBitCount;
    public ulong LowerMask { get; } = lowerMask;
    public ulong[] UpperBits { get; } = upperBits;
    public ulong[] LowerBits { get; } = lowerBits;
    public int UpperBitLength { get; } = upperBitLength;
    public int SampleRateShift { get; } = sampleRateShift;
    public int[] SamplePositions { get; } = samplePositions;
}
