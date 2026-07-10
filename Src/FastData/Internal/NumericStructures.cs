using Genbox.FastData.Config;
using Genbox.FastData.Generators.Extensions;
using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Internal;

internal static class NumericStructures<TKey>
{
    internal static Type GetBest(ReadOnlyMemory<TKey> keys, bool hasValues, float density, bool allowApproximate, int rangeCount, ulong range, StructureCapability reqCap, float denseIntegralValueMaxRangeFactor, StructureConfig config, Func<ReadOnlyMemory<TKey>, HashData> getHashData)
    {
        uint keyCount = (uint)keys.Length;

        TypeCode typeCode = Type.GetTypeCode(typeof(TKey));

        if (config.IsEnabled(typeof(SingleValueStructure<,>), reqCap) && keyCount == 1)
            return typeof(SingleValueStructure<,>);

        // Floating-point min/max ranges are not exact for sparse keys: [1.0, 3.0] would also accept 2.0.
        // Keep RangeStructure to integral keys where ranges represent discrete consecutive values.
        if (config.IsEnabled(typeof(RangeStructure<,>), reqCap) && typeCode.IsIntegral() && !hasValues && config.CheckItemCountLimits(typeof(RangeStructure<,>), (uint)rangeCount))
            return typeof(RangeStructure<,>);

        if (config.IsEnabled(typeof(BloomFilterStructure<,>), reqCap) && allowApproximate && !hasValues)
            return typeof(BloomFilterStructure<,>);

        if (hasValues && config.IsEnabled(typeof(BitSetStructure<,>), reqCap) && typeCode.IsIntegral() && config.CheckValueLimits(typeof(BitSetStructure<,>), range) &&
            IsDenseIntegralValueRangeAccepted(keyCount, range, denseIntegralValueMaxRangeFactor))
            return typeof(BitSetStructure<,>);

        if (!hasValues && config.IsEnabled(typeof(BitSetStructure<,>), reqCap) && typeCode.IsIntegral() && config.CheckValueLimits(typeof(BitSetStructure<,>), range) && config.CheckDensityLimits(typeof(BitSetStructure<,>), density))
            return typeof(BitSetStructure<,>);

        if (config.IsEnabled(typeof(ConditionalStructure<,>), reqCap) && config.CheckItemCountLimits(typeof(ConditionalStructure<,>), keyCount))
            return typeof(ConditionalStructure<,>);

        if (config.IsEnabled(typeof(RrrBitVectorStructure<,>), reqCap) && typeCode.IsIntegral() && !hasValues && config.CheckItemCountLimits(typeof(RrrBitVectorStructure<,>), keyCount) &&
            config.CheckDensityLimits(typeof(RrrBitVectorStructure<,>), density) && config.CheckValueLimits(typeof(RrrBitVectorStructure<,>), range))
            return typeof(RrrBitVectorStructure<,>);

        if (config.IsEnabled(typeof(EliasFanoStructure<,>), reqCap) && typeCode.IsIntegral() && !hasValues && config.CheckItemCountLimits(typeof(EliasFanoStructure<,>), keyCount) && config.CheckDensityLimits(typeof(EliasFanoStructure<,>), density))
            return typeof(EliasFanoStructure<,>);

        HashData hashData = getHashData(keys);

        if (config.IsEnabled(typeof(HashTablePerfectStructure<,>), reqCap) && hashData.HashCodesPerfect)
            return typeof(HashTablePerfectStructure<,>);

        if (config.IsEnabled(typeof(HybleStructure<,>), reqCap))
            return typeof(HybleStructure<,>);

        if (config.IsEnabled(typeof(HashTableStructure<,>), reqCap))
            return typeof(HashTableStructure<,>);

        if (config.IsEnabled(typeof(BinarySearchStructure<,>), reqCap))
            return typeof(BinarySearchStructure<,>);

        if (config.IsEnabled(typeof(ArrayStructure<,>), reqCap))
            return typeof(ArrayStructure<,>);

        throw new InvalidOperationException("No enabled numeric structure matched the requested configuration.");

        static bool IsDenseIntegralValueRangeAccepted(uint keyCount, ulong range, float maxRangeFactor)
        {
            if (float.IsNaN(maxRangeFactor) || float.IsInfinity(maxRangeFactor) || maxRangeFactor < 1f)
                return false;

            double slots = range + 1.0;
            return slots <= keyCount * (double)maxRangeFactor;
        }
    }
}