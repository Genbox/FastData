using Genbox.FastData.Config;
using Genbox.FastData.Generators.Extensions;
using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Internal;

internal static class NumericStructures<TKey>
{
    internal static Type GetBest(ReadOnlyMemory<TKey> keys,
                                 bool hasValues,
                                 float density,
                                 bool isConsecutive,
                                 bool allowApproximate,
                                 StructureConfig config,
                                 Func<ReadOnlyMemory<TKey>, HashData> getHashData)
    {
        uint keyCount = (uint)keys.Length;

        if (config.IsEnabled(typeof(SingleValueStructure<,>)) && keyCount == 1)
            return typeof(SingleValueStructure<,>);

        // RangeStructure handles consecutive keys, but does not support values
        if (config.IsEnabled(typeof(RangeStructure<,>)) && isConsecutive && !hasValues)
            return typeof(RangeStructure<,>);

        if (config.IsEnabled(typeof(BloomFilterStructure<,>)) && allowApproximate)
            return typeof(BloomFilterStructure<,>);

        TypeCode typeCode = Type.GetTypeCode(typeof(TKey));

        if (config.IsEnabled(typeof(BitSetStructure<,>)) && typeCode.IsIntegral() && config.CheckDensityLimits(typeof(BitSetStructure<,>), density))
            return typeof(BitSetStructure<,>);

        if (config.IsEnabled(typeof(ConditionalStructure<,>)) && config.CheckItemCountLimits(typeof(ConditionalStructure<,>), keyCount))
            return typeof(ConditionalStructure<,>);

        if (config.IsEnabled(typeof(RrrBitVectorStructure<,>)) && typeCode.IsIntegral() && !hasValues && config.CheckItemCountLimits(typeof(RrrBitVectorStructure<,>), keyCount) && config.CheckDensityLimits(typeof(RrrBitVectorStructure<,>), density))
            return typeof(RrrBitVectorStructure<,>);

        if (config.IsEnabled(typeof(EliasFanoStructure<,>)) && typeCode.IsIntegral() && !hasValues && config.CheckItemCountLimits(typeof(EliasFanoStructure<,>), keyCount) && config.CheckDensityLimits(typeof(EliasFanoStructure<,>), density))
            return typeof(EliasFanoStructure<,>);

        HashData hashData = getHashData(keys);

        if (config.IsEnabled(typeof(HashTablePerfectStructure<,>)) && hashData.HashCodesPerfect)
            return typeof(HashTablePerfectStructure<,>);

        if (config.IsEnabled(typeof(HashTableStructure<,>)))
            return typeof(HashTableStructure<,>);

        throw new InvalidOperationException("No enabled numeric structure matched the requested configuration.");
    }
}