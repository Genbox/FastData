using Genbox.FastData.Config;
using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Internal;

internal static class StringStructures
{
    internal static Type GetBest(ReadOnlyMemory<string> keys, bool hasValues, int minLength, int maxLength, bool allowApproximate, bool lengthsUnique, StructureConfig config, Func<ReadOnlyMemory<string>, HashData> getHashData)
    {
        uint keyCount = (uint)keys.Length;

        if (config.IsEnabled(typeof(SingleValueStructure<,>)) && keyCount == 1)
            return typeof(SingleValueStructure<,>);

        if (config.IsEnabled(typeof(BloomFilterStructure<,>)) && allowApproximate && !hasValues)
            return typeof(BloomFilterStructure<,>);

        float density = (float)keyCount / ((maxLength - minLength) + 1);

        if (config.IsEnabled(typeof(KeyLengthStructure<,>)) && lengthsUnique && config.CheckDensityLimits(typeof(KeyLengthStructure<,>), density))
            return typeof(KeyLengthStructure<,>);

        if (config.IsEnabled(typeof(ConditionalStructure<,>)) && config.CheckItemCountLimits(typeof(ConditionalStructure<,>), keyCount))
            return typeof(ConditionalStructure<,>);

        HashData hashData = getHashData(keys);

        if (config.IsEnabled(typeof(HashTablePerfectStructure<,>)) && hashData.HashCodesPerfect)
            return typeof(HashTablePerfectStructure<,>);

        if (config.IsEnabled(typeof(HybleStructure<,>)))
            return typeof(HybleStructure<,>);

        if (config.IsEnabled(typeof(HashTableStructure<,>)))
            return typeof(HashTableStructure<,>);

        throw new InvalidOperationException("No enabled string structure matched the requested configuration.");
    }
}