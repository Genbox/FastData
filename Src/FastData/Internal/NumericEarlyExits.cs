using System.Numerics;
using Genbox.FastData.Config;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits;
using Genbox.FastData.Generators.Extensions;

namespace Genbox.FastData.Internal;

internal static class NumericEarlyExits<TKey>
{
    internal static IEnumerable<IEarlyExit> GetCandidates(Type structureType, TKey min, TKey max, ulong range, ulong bitMask, uint itemCount, EarlyExitConfig config)
    {
        if (config.Disabled)
            yield break;

        if (!config.IsEnabledForStructure(structureType))
            yield break;

        // There is no point to using early exists if there is only a few items
        // This catches SingleStructure, and indirectly the case where min == max as well, because that means there is only one item.
        if (itemCount <= config.MinItemCount)
            yield break;

        float bitDensity = (float)BitOperations.PopCount(bitMask) / Type.GetTypeCode(typeof(TKey)).GetBitWidth();

        TypeCode typeCode = Type.GetTypeCode(typeof(TKey));

        if (config.IsEarlyExitEnabled(typeof(ValueBitMaskEarlyExit)) && typeCode.IsIntegral() && config.CheckDensityLimits(typeof(ValueBitMaskEarlyExit), bitDensity))
        {
            yield return new ValueBitMaskEarlyExit(bitMask);
            yield break;
        }

        float valueDensity = (float)range / itemCount;

        if (config.IsEarlyExitEnabled(typeof(ValueRangeEarlyExit<>)) && config.CheckDensityLimits(typeof(ValueRangeEarlyExit<>), valueDensity))
        {
            yield return new ValueRangeEarlyExit<TKey>(min, max);
            yield break;
        }
    }
}