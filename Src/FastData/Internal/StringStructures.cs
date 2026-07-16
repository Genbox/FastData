using Genbox.FastData.Config;
using Genbox.FastData.Enums;

namespace Genbox.FastData.Internal;

internal static class StringStructures
{
    internal static StructureType GetBest(ReadOnlyMemory<string> keys, bool hasValues, int minLength, int maxLength, bool allowApproximate, bool lengthsUnique, StructureCapability reqCap, StructureConfig config, Func<ReadOnlyMemory<string>, HashData> getHashData)
    {
        uint keyCount = (uint)keys.Length;

        if (config.IsEnabled(StructureType.SingleValue, reqCap) && keyCount == 1)
            return StructureType.SingleValue;

        if (config.IsEnabled(StructureType.BloomFilter, reqCap) && allowApproximate && !hasValues)
            return StructureType.BloomFilter;

        float density = (float)keyCount / ((maxLength - minLength) + 1);

        if (config.IsEnabled(StructureType.KeyLength, reqCap) && lengthsUnique && config.CheckDensityLimits(StructureType.KeyLength, density))
            return StructureType.KeyLength;

        if (config.IsEnabled(StructureType.Conditional, reqCap) && config.CheckItemCountLimits(StructureType.Conditional, keyCount))
            return StructureType.Conditional;

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

        throw new InvalidOperationException("No enabled string structure matched the requested configuration.");
    }
}