using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

public sealed class EliasFanoContext<TKey>(ReadOnlyMemory<TKey> keys, int lowerBitCount, ulong lowerMask, ulong[] upperBits, ulong[] lowerBits, int upperBitLength, int sampleRateShift, int[] samplePositions) : EliasFanoContext(lowerBitCount, lowerMask, upperBits, lowerBits, upperBitLength, sampleRateShift, samplePositions)
{
    public ReadOnlyMemory<TKey> Keys { get; } = keys;
}

public abstract class EliasFanoContext(int lowerBitCount, ulong lowerMask, ulong[] upperBits, ulong[] lowerBits, int upperBitLength, int sampleRateShift, int[] samplePositions) : IContext
{
    public int LowerBitCount { get; } = lowerBitCount;
    public ulong LowerMask { get; } = lowerMask;
    public ulong[] UpperBits { get; } = upperBits;
    public ulong[] LowerBits { get; } = lowerBits;
    public int UpperBitLength { get; } = upperBitLength;
    public int SampleRateShift { get; } = sampleRateShift;
    public int[] SamplePositions { get; } = samplePositions;
}