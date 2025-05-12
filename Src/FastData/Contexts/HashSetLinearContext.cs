using Genbox.FastData.Contexts.Misc;

namespace Genbox.FastData.Contexts;

public class HashSetLinearContext<T>(T[] data) : DefaultContext<T>(data)
{
    public HashSetLinearContext(T[] data, HashSetBucket[] buckets, ulong[] hashCodes) : this(data)
    {
        Buckets = buckets;
        HashCodes = hashCodes;
    }

    public HashSetBucket[] Buckets { get; }
    public ulong[] HashCodes { get; }
}