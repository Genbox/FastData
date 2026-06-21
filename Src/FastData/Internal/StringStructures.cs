using Genbox.FastData.Config;
using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Internal;

internal static class StringStructures
{
    internal static Type GetBest(ReadOnlyMemory<string> keys, bool hasValues, int minLength, int maxLength, bool allowApproximate, bool lengthsUnique, StructureCapability reqCap, StructureConfig config, Func<ReadOnlyMemory<string>, HashData> getHashData)
    {
        uint keyCount = (uint)keys.Length;

        if (config.IsEnabled(typeof(SingleValueStructure<,>), reqCap) && keyCount == 1)
            return typeof(SingleValueStructure<,>);

        if (config.IsEnabled(typeof(BloomFilterStructure<,>), reqCap) && allowApproximate && !hasValues)
            return typeof(BloomFilterStructure<,>);

        float density = (float)keyCount / ((maxLength - minLength) + 1);

        if (config.IsEnabled(typeof(KeyLengthStructure<,>), reqCap) && lengthsUnique && config.CheckDensityLimits(typeof(KeyLengthStructure<,>), density))
            return typeof(KeyLengthStructure<,>);

        if (config.IsEnabled(typeof(ConditionalStructure<,>), reqCap) && config.CheckItemCountLimits(typeof(ConditionalStructure<,>), keyCount))
            return typeof(ConditionalStructure<,>);

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

        throw new InvalidOperationException("No enabled string structure matched the requested configuration.");
    }
}