using Genbox.FastData.Config;
using Genbox.FastData.Internal.Structures;

namespace Genbox.FastData.Internal;

internal static class StringStructures
{
    internal static Type GetBest(ReadOnlyMemory<string> keys, bool hasValues, int minLength, int maxLength, bool allowApproximate, bool lengthsUnique, StructureConfig config, Func<ReadOnlyMemory<string>, HashData> getHashData)
    {
        uint keyCount = (uint)keys.Length;

        if (keyCount == 1)
            return typeof(SingleValueStructure<,>);

        if (allowApproximate && !hasValues)
            return typeof(BloomFilterStructure<,>);

        float density = (float)keyCount / ((maxLength - minLength) + 1);

        if (lengthsUnique && config.CheckDensityLimits(typeof(KeyLengthStructure<,>), density))
            return typeof(KeyLengthStructure<,>);

        if (config.CheckItemCountLimits(typeof(ConditionalStructure<,>), keyCount))
            return typeof(ConditionalStructure<,>);

        HashData hashData = getHashData(keys);

        if (hashData.HashCodesPerfect)
            return typeof(HashTablePerfectStructure<,>);

        return typeof(HashTableStructure<,>);
    }
}