using Genbox.FastData.Contexts.Misc;

namespace Genbox.FastData.Contexts;

public sealed class HashSetLinearContext<T>(T[] data, HashSetBucket[] buckets, ulong[] hashCodes) : DefaultContext<T>(data)
{
    public HashSetBucket[] Buckets { get; } = buckets;
    public ulong[] HashCodes { get; } = hashCodes;
}