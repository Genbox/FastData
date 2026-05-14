using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides Elias-Fano encoded data and original keys for generated structures.</summary>
public sealed class EliasFanoContext<TKey>(ReadOnlyMemory<TKey> keys, int lowerBitCount, ulong lowerMask, ulong[] upperBits, ulong[] lowerBits, int upperBitLength, int sampleRateShift, int[] samplePositions, long minValue, long maxValue) : EliasFanoContext(lowerBitCount, lowerMask, upperBits, lowerBits, upperBitLength, sampleRateShift, samplePositions, minValue, maxValue)
{
    /// <summary>Gets the sorted keys represented by the encoded data.</summary>
    public ReadOnlyMemory<TKey> Keys { get; } = keys;
}

/// <summary>Provides Elias-Fano encoded data for generated structures.</summary>
public abstract class EliasFanoContext(int lowerBitCount, ulong lowerMask, ulong[] upperBits, ulong[] lowerBits, int upperBitLength, int sampleRateShift, int[] samplePositions, long minValue, long maxValue) : IContext
{
    /// <summary>Gets the number of lower bits stored separately for each value.</summary>
    public int LowerBitCount { get; } = lowerBitCount;
    /// <summary>Gets the mask used to extract lower bits.</summary>
    public ulong LowerMask { get; } = lowerMask;
    /// <summary>Gets the encoded upper-bit stream.</summary>
    public ulong[] UpperBits { get; } = upperBits;
    /// <summary>Gets the packed lower-bit stream.</summary>
    public ulong[] LowerBits { get; } = lowerBits;
    /// <summary>Gets the number of valid bits in <see cref="UpperBits" />.</summary>
    public int UpperBitLength { get; } = upperBitLength;
    /// <summary>Gets the shift used to derive the sample rate.</summary>
    public int SampleRateShift { get; } = sampleRateShift;
    /// <summary>Gets sampled positions in the encoded upper-bit stream.</summary>
    public int[] SamplePositions { get; } = samplePositions;
    /// <summary>Gets the minimum original value represented by the structure.</summary>
    public long MinValue { get; } = minValue;
    /// <summary>Gets the maximum original value represented by the structure.</summary>
    public long MaxValue { get; } = maxValue;
}