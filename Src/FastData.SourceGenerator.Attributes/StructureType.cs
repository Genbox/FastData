using System.ComponentModel;

namespace Genbox.FastData.SourceGenerator.Attributes;

public enum StructureType : byte
{
    [Description("Selects the best structure automatically based on the input data.")]
    Auto = 0,

    [Description("Emits a linear scan over numeric or string keys.")]
    Array = 1,

    [Description("Sorts keys at generation time and emits binary-search logic.")]
    BinarySearch = 2,

    [Description("Uses numeric value distribution to estimate the next binary-search probe location.")]
    BinarySearchInterpolation = 3,

    [Description("Maps integral numeric keys to bit positions inside the observed range.")]
    BitSet = 4,

    [Description("Uses a compact approximate membership filter that can return false positives.")]
    BloomFilter = 5,

    [Description("Emits language-level conditions or switches for small datasets.")]
    Conditional = 6,

    [Description("Stores sparse monotonic integer sets in a compressed Elias-Fano representation.")]
    EliasFano = 7,

    [Description("Emits a general-purpose bucketed hash table for large or irregular datasets.")]
    HashTable = 8,

    [Description("Stores hash buckets and entries contiguously to reduce per-entry metadata.")]
    HashTableCompact = 9,

    [Description("Indexes directly by unique generated hash codes with no collision-chain metadata.")]
    HashTablePerfect = 10,

    [Description("Uses displacement-based perfect hashing with a generated seed and lookup table.")]
    Hyble = 11,

    [Description("Uses string length as the lookup index when every key length is unique.")]
    KeyLength = 12,

    [Description("Uses a generated PGM index for sorted numeric lookup.")]
    Pgm = 13,

    [Description("Stores consecutive numeric keys as one or more ranges.")]
    Range = 14,

    [Description("Stores very sparse integer sets as a compressed RRR bit vector.")]
    RrrBitVector = 15,

    [Description("Emits a direct equality check for a dataset with one unique key.")]
    SingleValue = 16,

    [Description("Uses a binary-fuse XOR table to recover and verify a candidate key index.")]
    ConstMap = 18
}