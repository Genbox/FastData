using Genbox.FastData.Models.Misc;

namespace Genbox.FastData.Models;

public class HashSetLinearContext(object[] data) : DefaultContext(data)
{
    public HashSetLinearContext(object[] data, HashSetBucket[] buckets, uint[] hashCodes) : this(data)
    {
        Buckets = buckets;
        HashCodes = hashCodes;
    }

    public HashSetBucket[] Buckets { get; }
    public uint[] HashCodes { get; }
}