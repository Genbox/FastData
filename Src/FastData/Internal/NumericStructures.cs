using Genbox.FastData.Config;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Extensions;

namespace Genbox.FastData.Internal;

internal static class NumericStructures<TKey>
{
    internal static StructureType GetBest(ReadOnlyMemory<TKey> keys, bool hasValues, float density, bool allowApproximate, int rangeCount, ulong range, StructureCapability reqCap, float denseIntegralValueMaxRangeFactor, StructureConfig config, Func<ReadOnlyMemory<TKey>, HashData> getHashData)
    {
        uint keyCount = (uint)keys.Length;

        TypeCode typeCode = Type.GetTypeCode(typeof(TKey));

        if (config.IsEnabled(StructureType.SingleValue, reqCap) && keyCount == 1)
            return StructureType.SingleValue;

        // Floating-point min/max ranges are not exact for sparse keys: [1.0, 3.0] would also accept 2.0.
        // Keep RangeStructure to integral keys where ranges represent discrete consecutive values.
        if (config.IsEnabled(StructureType.Range, reqCap) && typeCode.IsIntegral() && !hasValues && IsRangeCompressionAccepted(keyCount, rangeCount) &&
            config.CheckItemCountLimits(StructureType.Range, (uint)rangeCount))
            return StructureType.Range;

        if (config.IsEnabled(StructureType.BloomFilter, reqCap) && allowApproximate && !hasValues)
            return StructureType.BloomFilter;

        if (hasValues && config.IsEnabled(StructureType.BitSet, reqCap) && typeCode.IsIntegral() && config.CheckValueLimits(StructureType.BitSet, range) &&
            IsDenseIntegralValueRangeAccepted(keyCount, range, denseIntegralValueMaxRangeFactor))
            return StructureType.BitSet;

        if (!hasValues && config.IsEnabled(StructureType.BitSet, reqCap) && typeCode.IsIntegral() && config.CheckValueLimits(StructureType.BitSet, range) && config.CheckDensityLimits(StructureType.BitSet, density))
            return StructureType.BitSet;

        if (config.IsEnabled(StructureType.Conditional, reqCap) && config.CheckItemCountLimits(StructureType.Conditional, keyCount))
            return StructureType.Conditional;

        if (config.IsEnabled(StructureType.RrrBitVector, reqCap) && typeCode.IsIntegral() && !hasValues && config.CheckItemCountLimits(StructureType.RrrBitVector, keyCount) &&
            config.CheckDensityLimits(StructureType.RrrBitVector, density) && config.CheckValueLimits(StructureType.RrrBitVector, range))
            return StructureType.RrrBitVector;

        if (config.IsEnabled(StructureType.EliasFano, reqCap) && typeCode.IsIntegral() && !hasValues && config.CheckItemCountLimits(StructureType.EliasFano, keyCount) && config.CheckDensityLimits(StructureType.EliasFano, density))
            return StructureType.EliasFano;

        HashData hashData = getHashData(keys);

        if (config.IsEnabled(StructureType.HashTablePerfect, reqCap) && hashData.HashCodesPerfect)
            return StructureType.HashTablePerfect;

        if (config.IsEnabled(StructureType.Hyble, reqCap))
            return StructureType.Hyble;

        if (config.IsEnabled(StructureType.HashTable, reqCap))
            return StructureType.HashTable;

        if (config.IsEnabled(StructureType.BinarySearch, reqCap))
            return StructureType.BinarySearch;

        if (config.IsEnabled(StructureType.Array, reqCap))
            return StructureType.Array;

        throw new InvalidOperationException("No enabled numeric structure matched the requested configuration.");

        static bool IsDenseIntegralValueRangeAccepted(uint keyCount, ulong range, float maxRangeFactor)
        {
            if (float.IsNaN(maxRangeFactor) || float.IsInfinity(maxRangeFactor) || maxRangeFactor < 1f)
                return false;

            double slots = range + 1.0;
            return slots <= keyCount * (double)maxRangeFactor;
        }

        static bool IsRangeCompressionAccepted(uint keyCount, int rangeCount)
        {
            // A single range is emitted as two constants. Multiple ranges use two endpoints each.
            return rangeCount == 1 || (ulong)(uint)rangeCount * 2UL < keyCount;
        }
    }
}