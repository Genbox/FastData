using System.Numerics;
using Genbox.FastData.Config;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Exits;
using Genbox.FastData.Generators.Extensions;
using Genbox.FastData.Internal.Analysis.Data;

namespace Genbox.FastData.Generators.EarlyExits;

internal static class NumericEarlyExits<TKey>
{
    public static IEarlyExit[] GetExits(Type structureType, DataRanges<TKey> dataRanges, ulong range, ulong bitMask, uint itemCount, EarlyExitConfig config)
    {
        // First we build a set of candidates.
        IEarlyExit[] candidates = ProduceCandidates(structureType, dataRanges, range, bitMask, itemCount, config).ToArray();

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

    private static IEnumerable<IEarlyExit> ProduceCandidates(Type structureType, DataRanges<TKey> dataRanges, ulong range, ulong bitMask, uint itemCount, EarlyExitConfig config)
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

            //TODO: Find smaller ranges (even ranges of 1) and pack them into bitmaps

            yield return new ValueInRangeEarlyExit<TKey>(current.End, next.Start);
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
        double ratio = exit is ValueBitMaskEarlyExit bitMask ? 1d - GetBitMaskAcceptedDensity(bitMask.Mask) :
            domainSize <= 0d ? 0d : exit.KeyspaceSize / domainSize;
        return ClampRatio(ratio);
    }

    private static double GetEstimatedCost(IEarlyExit exit) => exit switch
    {
        ValueInRangeEarlyExit<TKey> => 2d,
        ValueBitMaskEarlyExit => 1.25d,
        _ => 1d
    };

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