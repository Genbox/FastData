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

        TypeCode typeCode = Type.GetTypeCode(typeof(TKey));

        if (config.IsEarlyExitEnabled(typeof(ValueBitMaskEarlyExit)) && typeCode.IsIntegral())
        {
            float density = (float)range / itemCount;

            if (config.CheckDensityLimits(typeof(ValueBitMaskEarlyExit), density))
                yield return new ValueBitMaskEarlyExit(bitMask);
        }

        if (config.IsEarlyExitEnabled(typeof(ValueNotEqualEarlyExit<>)) && EqualityComparer<TKey>.Default.Equals(min, max))
            yield return new ValueNotEqualEarlyExit<TKey>(min);

        if (config.IsEarlyExitEnabled(typeof(ValueLessThanEarlyExit<>)) && Comparer<TKey>.Default.Compare(min, typeCode.GetMinValue<TKey>()) > 0)
            yield return new ValueLessThanEarlyExit<TKey>(min);

        if (config.IsEarlyExitEnabled(typeof(ValueGreaterThanEarlyExit<>)) && Comparer<TKey>.Default.Compare(max, typeCode.GetMaxValue<TKey>()) < 0)
            yield return new ValueGreaterThanEarlyExit<TKey>(max);
    }
}