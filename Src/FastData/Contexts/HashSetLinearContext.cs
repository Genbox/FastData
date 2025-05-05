using Genbox.FastData.Contexts.Misc;

namespace Genbox.FastData.Contexts;

public class HashSetLinearContext<T>(T[] data) : DefaultContext<T>(data)
{
    public HashSetLinearContext(T[] data, HashSetBucket[] buckets, uint[] hashCodes) : this(data)
    {
        Buckets = buckets;
        HashCodes = hashCodes;
    }

    public HashSetBucket[] Buckets { get; }
    public uint[] HashCodes { get; }
}