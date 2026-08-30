using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Genbox.FastData.SourceGenerator.Attributes;

/// <summary>Specifies the data structure generated for the declared key set.</summary>
[SuppressMessage("Design", "CA1027:Mark enums with FlagsAttribute", Justification = "Each value selects exactly one structure; values are sequential identifiers, not combinable flags.")]
public enum StructureType : byte
{
    /// <summary>Selects the best structure automatically based on the input data.</summary>
    [Description("Selects the best structure automatically based on the input data.")]
    Auto = 0,

    /// <summary>Emits a linear scan over numeric or string keys.</summary>
    [Description("Emits a linear scan over numeric or string keys.")]
    Array = 1,

    /// <summary>Sorts keys at generation time and emits binary-search logic.</summary>
    [Description("Sorts keys at generation time and emits binary-search logic.")]
    BinarySearch = 2,

    /// <summary>Uses the numeric distribution to estimate each binary-search probe.</summary>
    [Description("Uses numeric value distribution to estimate the next binary-search probe location.")]
    BinarySearchInterpolation = 3,

    /// <summary>Maps integral keys to bit positions inside the observed range.</summary>
    [Description("Maps integral numeric keys to bit positions inside the observed range.")]
    BitSet = 4,

    /// <summary>Uses an approximate membership filter that can return false positives.</summary>
    [Description("Uses a compact approximate membership filter that can return false positives.")]
    BloomFilter = 5,

    /// <summary>Emits language-level conditions or switches for small datasets.</summary>
    [Description("Emits language-level conditions or switches for small datasets.")]
    Conditional = 6,

    /// <summary>Stores sparse monotonic integer sets in an Elias-Fano representation.</summary>
    [Description("Stores sparse monotonic integer sets in a compressed Elias-Fano representation.")]
    EliasFano = 7,

    /// <summary>Emits a general-purpose bucketed hash table.</summary>
    [Description("Emits a general-purpose bucketed hash table for large or irregular datasets.")]
    HashTable = 8,

    /// <summary>Stores hash buckets and entries contiguously.</summary>
    [Description("Stores hash buckets and entries contiguously to reduce per-entry metadata.")]
    HashTableCompact = 9,

    /// <summary>Indexes directly by unique generated hash codes.</summary>
    [Description("Indexes directly by unique generated hash codes with no collision-chain metadata.")]
    HashTablePerfect = 10,

    /// <summary>Uses displacement-based perfect hashing.</summary>
    [Description("Uses displacement-based perfect hashing with a generated seed and lookup table.")]
    Hyble = 11,

    /// <summary>Uses string length as the lookup index when lengths are unique.</summary>
    [Description("Uses string length as the lookup index when every key length is unique.")]
    KeyLength = 12,

    /// <summary>Uses a generated PGM index for sorted numeric lookup.</summary>
    [Description("Uses a generated PGM index for sorted numeric lookup.")]
    Pgm = 13,

    /// <summary>Stores consecutive numeric keys as ranges.</summary>
    [Description("Stores consecutive numeric keys as one or more ranges.")]
    Range = 14,

    /// <summary>Stores sparse integer sets as a compressed RRR bit vector.</summary>
    [Description("Stores very sparse integer sets as a compressed RRR bit vector.")]
    RrrBitVector = 15,

    /// <summary>Emits a direct equality check for one unique key.</summary>
    [Description("Emits a direct equality check for a dataset with one unique key.")]
    SingleValue = 16,

    /// <summary>Uses a binary-fuse XOR table to recover and verify a key index.</summary>
    [Description("Uses a binary-fuse XOR table to recover and verify a candidate key index.")]
    ConstMap = 18
}