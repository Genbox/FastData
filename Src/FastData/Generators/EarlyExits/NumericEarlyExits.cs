using System.Numerics;
using Genbox.FastData.Config;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Exits;
using Genbox.FastData.Generators.Extensions;
using Genbox.FastData.Internal.Analysis.Data;
using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Generators.EarlyExits;

internal static class NumericEarlyExits<TKey>
{
    public static IEarlyExit[] GetExits(Type structureType, ReadOnlyMemory<TKey> keys, DataRanges<TKey> dataRanges, ulong range, ulong bitMask, uint itemCount, EarlyExitConfig config)
    {
        // First we build a set of candidates.
        IEarlyExit[] candidates = ProduceCandidates(structureType, keys, dataRanges, bitMask, itemCount, config).ToArray();

        // If the user turned off early exits, or none was produced, we exit here.
        if (candidates.Length == 0)
            return [];

        float threshold = config.MinRejectionRatio;
        if (threshold > 0f)
            candidates = FilterByRejection(candidates, range, threshold).ToArray();

        if (candidates.Length == 0)
            return [];

        // There can be quite a few candidates, and too many will slow down queries, so we need to find the best ones.
        return GetTopExits(candidates, range, config.MaxCandidates).ToArray();
    }

    private static IEnumerable<IEarlyExit> ProduceCandidates(Type structureType, ReadOnlyMemory<TKey> keys, DataRanges<TKey> dataRanges, ulong bitMask, uint itemCount, EarlyExitConfig config)
    {
        if (config.Disabled)
            yield break;

        if (!config.IsEnabledForStructure(structureType))
            yield break;

        // There is no point to using early exists if there is only a few items
        // This catches SingleStructure, and indirectly the case where min == max as well, because that means there is only one item.
        if (itemCount <= config.MinItemCount)
            yield break;

        TypeCode typeCode = Type.GetTypeCode(typeof(TKey));

        if (config.IsEarlyExitEnabled(typeof(ValueLowByteBitmapEarlyExit)) && typeCode.IsIntegral() && structureType != typeof(BitSetStructure<,>))
        {
            ValueLowByteBitmapEarlyExit? lowByteExit = CreateLowByteBitmapExit(keys, typeCode);
            if (lowByteExit != null && config.CheckDensityLimits(typeof(ValueLowByteBitmapEarlyExit), lowByteExit.AcceptedDensity))
                yield return lowByteExit;
        }

        // Represents a mask like 01010011010100 where all the ones are bits that are not set in the input values.
        // When there are too many values, the bitset quickly becomes all ones and the check becomes useless.
        if (config.IsEarlyExitEnabled(typeof(ValueBitMaskEarlyExit)) && typeCode.IsIntegral() && bitMask != 0 && bitMask != ulong.MaxValue)
        {
            float density = GetBitMaskAcceptedDensity(bitMask);

            if (config.CheckDensityLimits(typeof(ValueBitMaskEarlyExit), density))
                yield return new ValueBitMaskEarlyExit(bitMask);
        }

        // These limits are designed to check for values outside the observed bounds. For example, if all values fall into the range [100..200]
        // the LessThan checks if "x < 100" and GreaterThan "x > 200"
        TKey min = dataRanges.Min;
        if (config.IsEarlyExitEnabled(typeof(ValueLessThanEarlyExit<>)) && Comparer<TKey>.Default.Compare(min, typeCode.GetMinValue<TKey>()) > 0)
            yield return new ValueLessThanEarlyExit<TKey>(min);

        TKey max = dataRanges.Max;
        if (config.IsEarlyExitEnabled(typeof(ValueGreaterThanEarlyExit<>)) && Comparer<TKey>.Default.Compare(max, typeCode.GetMaxValue<TKey>()) < 0)
            yield return new ValueGreaterThanEarlyExit<TKey>(max);

        // Less/GreaterThan does not cover ranges within the observed values. Instead, we use the RLE map coming from KeyAnalyzer to determine
        // where there is data, and build a set of empty ranges (where there is no data), which we can use as early exits.
        // Gaps can consist of a range of values, or singletons (a range where start == end).
        for (int i = 0; i < dataRanges.Ranges.Count - 1; i++)
        {
            (TKey Start, TKey End) current = dataRanges.Ranges[i];
            (TKey Start, TKey End) next = dataRanges.Ranges[i + 1];

            yield return new ValueInRangeEarlyExit<TKey>(current.End, next.Start);
        }

        // Pack consecutive small gaps into a single bitmap check when they fit within 64 positions.
        // This covers cases where many small gaps (including singletons) are close together.
        if (config.IsEarlyExitEnabled(typeof(ValueBitSetEarlyExit<>)) && typeCode.IsIntegral() && dataRanges.Ranges.Count > 2)
        {
            foreach (IEarlyExit packed in PackGapsIntoBitmap(dataRanges))
                yield return packed;
        }
    }

    private static IEnumerable<IEarlyExit> PackGapsIntoBitmap(DataRanges<TKey> dataRanges)
    {
        // Collect all gap values (values NOT in any data range) between consecutive ranges.
        // Then try to fit consecutive gap regions into a single 64-bit bitmap.
        TypeCode typeCode = Type.GetTypeCode(typeof(TKey));

        // Build a converter from unsigned back to TKey. For unsigned types we use the direct converter,
        // for signed types we need to go through the signed converter with unchecked semantics.
        Func<ulong, TKey> fromUlong;
        Func<TKey, ulong> toUlong = typeCode.GetUnsignedValueConverter<TKey>();

        if (typeCode.IsUnsigned())
        {
            fromUlong = typeCode.GetUnsignedKeyConverter<TKey>();
        }
        else
        {
            Func<long, TKey> fromSigned = typeCode.GetSignedKeyConverter<TKey>();
            fromUlong = v => fromSigned(unchecked((long)v));
        }

        // Build a list of (gapStart, gapEnd) in unsigned space, where gapStart/gapEnd are the exclusive bounds of data ranges.
        // The actual missing values are the integers strictly between current.End and next.Start.
        List<(ulong Start, ulong End)> gapRegions = new List<(ulong, ulong)>();

        for (int i = 0; i < dataRanges.Ranges.Count - 1; i++)
        {
            ulong gapStart = toUlong(dataRanges.Ranges[i].End);
            ulong gapEnd = toUlong(dataRanges.Ranges[i + 1].Start);

            // The missing values are (gapStart, gapEnd) exclusive, i.e., gapStart+1 to gapEnd-1.
            if (unchecked(gapEnd - gapStart) > 1)
                gapRegions.Add((gapStart, gapEnd));
        }

        if (gapRegions.Count < 2)
            yield break;

        // Try sliding windows of consecutive gap regions that fit within 64 positions.
        for (int start = 0; start < gapRegions.Count; start++)
        {
            ulong bitmapStart = gapRegions[start].Start + 1; // First missing value
            ulong missingBitSet = 0;
            int gapsIncluded = 0;

            for (int end = start; end < gapRegions.Count; end++)
            {
                ulong lastMissing = gapRegions[end].End - 1; // Last missing value in this gap
                ulong span = unchecked(lastMissing - bitmapStart);

                if (span >= 64)
                    break;

                // Add all missing values from this gap to the bitmap
                ulong gapFirst = gapRegions[end].Start + 1;
                ulong gapLast = gapRegions[end].End - 1;

                for (ulong v = gapFirst; v <= gapLast; v++)
                {
                    int bit = (int)unchecked(v - bitmapStart);
                    missingBitSet |= 1UL << bit;
                }

                gapsIncluded = end - start + 1;
            }

            // Only emit a bitmap if it covers at least 2 gap regions (otherwise the individual ValueInRangeEarlyExit is simpler)
            if (gapsIncluded >= 2 && missingBitSet != 0)
            {
                ulong lastGapEnd = gapRegions[start + gapsIncluded - 1].End;
                TKey bitmapStartKey = fromUlong(bitmapStart);
                TKey bitmapEndKey = fromUlong(lastGapEnd - 1);
                yield return new ValueBitSetEarlyExit<TKey>(bitmapStartKey, bitmapEndKey, missingBitSet);

                // Skip past the gaps we just packed
                start += gapsIncluded - 1;
            }
        }
    }

    private static IEnumerable<IEarlyExit> GetTopExits(IEarlyExit[] candidates, ulong range, int maxCandidates)
    {
        if (maxCandidates <= 0 || candidates.Length == 0)
            return [];

        return candidates.OrderByDescending(x => GetScore(x, range)).Take(maxCandidates);
    }

    private static IEnumerable<IEarlyExit> FilterByRejection(IEarlyExit[] candidates, ulong range, float threshold)
    {
        double domainSize = range + 1d;

        foreach (IEarlyExit exit in candidates)
        {
            double ratio = GetRejectionRatio(exit, domainSize);

            if (ratio >= threshold)
                yield return exit;
        }
    }

    private static double GetScore(IEarlyExit exit, ulong range)
    {
        double domainSize = range + 1d;
        return GetRejectionRatio(exit, domainSize) / GetEstimatedCost(exit);
    }

    private static double GetRejectionRatio(IEarlyExit exit, double domainSize)
    {
        double ratio;
        if (exit is ValueBitMaskEarlyExit bitMask)
            ratio = 1d - GetBitMaskAcceptedDensity(bitMask.Mask);
        else if (exit is ValueLowByteBitmapEarlyExit lowByte)
            ratio = lowByte.KeyspaceSize / 256d;
        else
            ratio = domainSize <= 0d ? 0d : exit.KeyspaceSize / domainSize;

        return ClampRatio(ratio);
    }

    private static double GetEstimatedCost(IEarlyExit exit) => exit switch
    {
        ValueInRangeEarlyExit<TKey> => 2d,
        ValueBitSetEarlyExit<TKey> => 3d,
        ValueLowByteBitmapEarlyExit => 4d,
        ValueBitMaskEarlyExit => 1.25d,
        _ => 1d
    };

    private static ValueLowByteBitmapEarlyExit? CreateLowByteBitmapExit(ReadOnlyMemory<TKey> keys, TypeCode typeCode)
    {
        Func<TKey, ulong> toUlong = typeCode.GetUnsignedValueConverter<TKey>();
        ulong word0 = 0;
        ulong word1 = 0;
        ulong word2 = 0;
        ulong word3 = 0;

        foreach (TKey key in keys.Span)
        {
            int bucket = (int)(toUlong(key) & 0xffUL);
            ulong bit = 1UL << (bucket & 63);

            switch (bucket >> 6)
            {
                case 0: word0 |= bit; break;
                case 1: word1 |= bit; break;
                case 2: word2 |= bit; break;
                case 3: word3 |= bit; break;
            }
        }

        return word0 == ulong.MaxValue && word1 == ulong.MaxValue && word2 == ulong.MaxValue && word3 == ulong.MaxValue
            ? null
            : new ValueLowByteBitmapEarlyExit(word0, word1, word2, word3);
    }

    private static float GetBitMaskAcceptedDensity(ulong bitMask)
    {
        int missingBitCount = BitOperations.PopCount(bitMask);
        if (missingBitCount >= 64)
            return 0f;

        return 1f / (1UL << missingBitCount);
    }

    private static double ClampRatio(double ratio)
    {
        if (double.IsNaN(ratio) || double.IsInfinity(ratio) || ratio > 1d)
            return 1d;

        if (ratio < 0d)
            return 0d;

        return ratio;
    }
}