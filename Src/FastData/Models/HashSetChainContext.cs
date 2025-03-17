using Genbox.FastData.Models.Misc;

namespace Genbox.FastData.Models;

public class HashSetChainContext(object[] data) : DefaultContext(data)
{
    public HashSetChainContext(object[] data, int[] buckets, HashSetEntry[] entries) : this(data)
    {
        Buckets = buckets;
        Entries = entries;
    }

    public int[] Buckets { get; }
    public HashSetEntry[] Entries { get; }
}