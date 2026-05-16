using Genbox.FastData.Generators.Abstracts;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides a context for Hyble displacement-based perfect hash structures.</summary>
public sealed class HybleContext<TKey, TValue>(KeyValuePair<TKey, ulong>[] data, ushort[] displacements, uint approxRange, uint bucketMask, ulong seed, ReadOnlyMemory<TValue> values) : HybleContext(displacements, approxRange, bucketMask, seed)
{
    /// <summary>Gets the array of keys indexed by final displacement-computed position.</summary>
    public KeyValuePair<TKey, ulong>[] Data { get; } = data;

    /// <summary>Gets the values reordered to match entry positions.</summary>
    public ReadOnlyMemory<TValue> Values { get; } = values;
}

/// <summary>Provides metadata shared by Hyble-generated structures.</summary>
public abstract class HybleContext(ushort[] displacements, uint approxRange, uint bucketMask, ulong seed) : IContext
{
    /// <summary>Gets the per-bucket displacement values.</summary>
    public ushort[] Displacements { get; } = displacements;

    /// <summary>Gets the approximate range used for multiply-high reduction.</summary>
    public uint ApproxRange { get; } = approxRange;

    /// <summary>Gets the bitmask for bucket selection (bucketCount - 1).</summary>
    public uint BucketMask { get; } = bucketMask;

    /// <summary>
    /// The seed multiplied into the base hash during construction. The generated hash function must
    /// emit <c>hash(key) * Seed</c> to reproduce the same approx/bucket mapping at query time.
    /// </summary>
    public ulong Seed { get; } = seed;
}