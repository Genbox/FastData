using System.Numerics;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;

namespace Genbox.FastData.Internal.Structures;

internal sealed class EliasFanoStructure<TKey, TValue>(NumericKeyProperties<TKey> props, FastDataConfig cfg, bool keysAreSorted = false) : IStructure<TKey, TValue, EliasFanoContext<TKey>>
{
    public EliasFanoContext<TKey> Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        //We need the data to be sorted
        if (!keysAreSorted)
        {
            TKey[] keysCopy = new TKey[keys.Length];
            keys.CopyTo(keysCopy);
            Array.Sort(keysCopy);
            keys = keysCopy.AsMemory();
        }

        ReadOnlySpan<TKey> keysSpan = keys.Span;

        int count = keysSpan.Length;
        long maxValue = props.ValueConverter(keysSpan[count - 1]);

        long factor = maxValue / count;
        int lowerBitCount = factor <= 0 ? 0 : BitOperations.Log2((ulong)factor);
        int upperBitLength = (int)(count + (maxValue >> lowerBitCount));
        ulong[] upperBits = new ulong[(upperBitLength + 63) / 64];
        ulong[] lowerBits = new ulong[((count * lowerBitCount) + 63) / 64];
        ulong lowerMask = 0;

        //Small optimization: If there are no lower bits, we can simply operate on upper bits.
        if (lowerBitCount == 0)
        {
            for (int i = 0; i < keysSpan.Length; i++)
            {
                long value = props.ValueConverter(keysSpan[i]);
                int index = (int)(value + i);
                upperBits[index >> 6] |= 1UL << (index & 63);
            }
        }
        else
        {
            lowerMask = (1UL << lowerBitCount) - 1;

            for (int i = 0; i < keysSpan.Length; i++)
            {
                long value = props.ValueConverter(keysSpan[i]);
                int index = (int)((value >> lowerBitCount) + i);
                upperBits[index >> 6] |= 1UL << (index & 63);

                long bitPosition = (long)i * lowerBitCount;
                int block = (int)(bitPosition >> 6);
                int shift = (int)(bitPosition & 63);

                ulong low = (ulong)value & lowerMask;
                lowerBits[block] |= low << shift;

                if (shift + lowerBitCount > 64)
                {
                    int carry = 64 - shift;
                    lowerBits[block + 1] |= low >> carry;
                }
            }
        }

        //To index Elias-Fano, we build a quantum skip list
        int skipQuantum = cfg.SkipQuantum;
        int sampleRateShift = BitOperations.TrailingZeroCount((uint)skipQuantum);
        int[] samplePositions = BuildSamples(upperBits, upperBitLength, skipQuantum);

        return new EliasFanoContext<TKey>(keys, lowerBitCount, lowerMask, upperBits, lowerBits, upperBitLength, sampleRateShift, samplePositions);
    }

    private static int[] BuildSamples(ulong[] words, int bitLength, int sampleRate)
    {
        if (bitLength <= 0)
            return [];

        long zeroCount = CountZeros(words, bitLength);
        if (zeroCount <= 0)
            return [];

        int sampleCount = (int)(((zeroCount - 1) / sampleRate) + 1);
        int[] samples = new int[sampleCount];
        samples[0] = 0;

        int sampleIndex = 1;
        long nextSampleRank = sampleRate;
        long seenZeros = 0;

        for (int wordIndex = 0; wordIndex < words.Length && sampleIndex < samples.Length; wordIndex++)
        {
            int validBits = GetValidBits(wordIndex, words.Length, bitLength);
            if (validBits <= 0)
                break;

            ulong validMask = validBits == 64 ? ulong.MaxValue : (1UL << validBits) - 1;
            ulong zeros = ~words[wordIndex] & validMask;

            while (zeros != 0 && sampleIndex < samples.Length)
            {
                int bit = BitOperations.TrailingZeroCount(zeros);
                seenZeros++;

                if (seenZeros - 1 == nextSampleRank)
                {
                    samples[sampleIndex++] = (wordIndex << 6) + bit;
                    nextSampleRank += sampleRate;
                }

                zeros &= zeros - 1;
            }
        }

        return samples;
    }

    private static long CountZeros(ulong[] words, int bitLength)
    {
        long zeros = 0;

        for (int i = 0; i < words.Length; i++)
        {
            int validBits = GetValidBits(i, words.Length, bitLength);
            if (validBits <= 0)
                break;

            ulong validMask = validBits == 64 ? ulong.MaxValue : (1UL << validBits) - 1;
            ulong zerosWord = ~words[i] & validMask;
            zeros += BitOperations.PopCount(zerosWord);
        }

        return zeros;
    }

    private static int GetValidBits(int wordIndex, int wordCount, int bitLength)
    {
        if (wordIndex != wordCount - 1)
            return 64;

        int remainder = bitLength & 63;
        return remainder == 0 ? 64 : remainder;
    }
}