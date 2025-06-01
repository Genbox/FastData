using Genbox.FastData.Contexts.Misc;

namespace Genbox.FastData.Contexts;

public sealed class HashSetChainContext<T>(T[] data, int[] buckets, HashSetEntry<T>[] entries) : DefaultContext<T>(data)
{
    public int[] Buckets { get; } = buckets;
    public HashSetEntry<T>[] Entries { get; } = entries;
}