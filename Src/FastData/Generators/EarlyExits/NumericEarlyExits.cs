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

        // There can be quite a few candidates, and too many will slow down queries, so we need to find the best ones.
        return GetTopExits(candidates, config.MaxCandidates).ToArray();
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
            float density = (float)range / itemCount;

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

    private static IEnumerable<IEarlyExit> GetTopExits(IEarlyExit[] candidates, int maxCandidates)
    {
        if (maxCandidates <= 0 || candidates.Length == 0)
            return [];

        return candidates.OrderByDescending(x => x.KeyspaceSize).Take(maxCandidates);
    }
}