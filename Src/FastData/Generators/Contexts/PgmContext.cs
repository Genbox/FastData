using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Internal.Pgm;

namespace Genbox.FastData.Generators.Contexts;

/// <summary>Provides precomputed PGM index segments alongside sorted key and value data for PGM-index generated structures.</summary>
public sealed class PgmContext<TKey, TValue>(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values, PgmSegment<TKey>[] segments, int[] levelsOffsets, int epsilon, int epsilonRecursive, int segmentCount) : IContext
{
    /// <summary>Gets the sorted keys emitted into the generated structure.</summary>
    public ReadOnlyMemory<TKey> Keys { get; } = keys;

    /// <summary>Gets the values emitted into the generated structure.</summary>
    public ReadOnlyMemory<TValue> Values { get; } = values;

    /// <summary>Gets the precomputed PGM segments.</summary>
    public PgmSegment<TKey>[] Segments { get; } = segments;

    /// <summary>Gets the starting offset of each PGM index level in <see cref="Segments" />.</summary>
    public int[] LevelsOffsets { get; } = levelsOffsets;

    /// <summary>Gets the epsilon used during construction. Determines the search range in the generated code.</summary>
    public int Epsilon { get; } = epsilon;

    /// <summary>Gets the epsilon used for recursive PGM index levels.</summary>
    public int EpsilonRecursive { get; } = epsilonRecursive;

    /// <summary>Gets the number of segments in the index.</summary>
    public int SegmentCount { get; } = segmentCount;
}