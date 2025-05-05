using Genbox.FastData.Contexts.Misc;

namespace Genbox.FastData.Contexts;

public class HashSetChainContext<T>(T[] data) : DefaultContext<T>(data)
{
    public HashSetChainContext(T[] data, int[] buckets, HashSetEntry<T>[] entries) : this(data)
    {
        Buckets = buckets;
        Entries = entries;
    }

    public int[] Buckets { get; }
    public HashSetEntry<T>[] Entries { get; }
}