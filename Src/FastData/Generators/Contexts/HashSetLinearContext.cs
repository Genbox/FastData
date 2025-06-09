using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts.Misc;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for hash set linear probing-based data structures.</summary>
/// <typeparam name="T">The type of elements in the data array.</typeparam>
/// <param name="data">The data array.</param>
/// <param name="buckets">The array of hash set buckets.</param>
/// <param name="hashCodes">The array of hash codes.</param>
public sealed class HashSetLinearContext<T>(T[] data, HashSetBucket[] buckets, ulong[] hashCodes) : IContext<T>
{
    public T[] Data { get; } = data;

    /// <summary>Gets the array of hash set buckets.</summary>
    public HashSetBucket[] Buckets { get; } = buckets;
    /// <summary>Gets the array of hash codes.</summary>
    public ulong[] HashCodes { get; } = hashCodes;
}