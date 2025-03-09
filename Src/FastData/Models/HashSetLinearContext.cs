using Genbox.FastData.Models.Misc;

namespace Genbox.FastData.Models;

public class HashSetLinearContext(object[] data) : DefaultContext(data)
{
    public HashSetLinearContext(object[] data, Bucket[] buckets, uint[] hashCodes) : this(data)
    {
        Buckets = buckets;
        HashCodes = hashCodes;
    }

    public Bucket[] Buckets { get; }
    public uint[] HashCodes { get; }
}