using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts.Misc;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for hash set chain-based data structure.</summary>
/// <typeparam name="T">The type of elements in the data array.</typeparam>
/// <param name="buckets">The array of bucket indices.</param>
/// <param name="entries">The array of hash set entries.</param>
public sealed class HashSetChainContext<T>(int[] buckets, HashSetEntry<T>[] entries) : IContext<T>
{
    /// <summary>Gets the array of bucket indices.</summary>
    public int[] Buckets { get; } = buckets;

    /// <summary>Gets the array of hash set entries.</summary>
    public HashSetEntry<T>[] Entries { get; } = entries;
}