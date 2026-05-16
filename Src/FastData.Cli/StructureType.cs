using System.ComponentModel;

#if SourceGen
namespace Genbox.FastData.SourceGenerator.Attributes;
#else
namespace Genbox.FastData.Cli;
#endif

public enum StructureType : byte
{
    [Description("Selects the best structure automatically based on the input data.")]
    Auto = 0,

    [Description("Emits a linear scan over numeric or string keys.")]
    Array,

    [Description("Sorts keys at generation time and emits binary-search logic.")]
    BinarySearch,

    [Description("Uses numeric value distribution to estimate the next binary-search probe location.")]
    BinarySearchInterpolation,

    [Description("Maps integral numeric keys to bit positions inside the observed range.")]
    BitSet,

    [Description("Uses a compact approximate membership filter that can return false positives.")]
    BloomFilter,

    [Description("Emits language-level conditions or switches for small datasets.")]
    Conditional,

    [Description("Stores sparse monotonic integer sets in a compressed Elias-Fano representation.")]
    EliasFano,

    [Description("Stores hash buckets and entries contiguously to reduce per-entry metadata.")]
    HashTableCompact,

    [Description("Indexes directly by unique generated hash codes with no collision-chain metadata.")]
    HashTablePerfect,

    [Description("Emits a general-purpose bucketed hash table for large or irregular datasets.")]
    HashTable,

    [Description("Uses displacement-based perfect hashing with a generated seed and lookup table.")]
    Hyble,

    [Description("Uses string length as the lookup index when every key length is unique.")]
    KeyLength,

    [Description("Uses a generated PGM index for sorted numeric lookup.")]
    Pgm,

    [Description("Stores consecutive numeric keys as one or more ranges.")]
    Range,

    [Description("Stores very sparse integer sets as a compressed RRR bit vector.")]
    RrrBitVector,

    [Description("Emits a direct equality check for a dataset with one unique key.")]
    SingleValue
}