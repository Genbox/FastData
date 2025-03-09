using Genbox.FastData.Models.Misc;

namespace Genbox.FastData.Models;

public class HashSetChainContext(object[] data) : DefaultContext(data)
{
    public HashSetChainContext(object[] data, int[] buckets, Entry[] entries) : this(data)
    {
        Buckets = buckets;
        Entries = entries;
    }

    public int[] Buckets { get; }
    public Entry[] Entries { get; }
}