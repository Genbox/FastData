namespace Genbox.FastData.Cli;

public enum StructureType : byte
{
    Auto = 0,

    Array,
    BinarySearch,
    BinarySearchInterpolation,
    BitSet,
    BloomFilter,
    Conditional,
    EliasFano,
    HashTableCompact,
    HashTablePerfect,
    HashTable,
    KeyLength,
    Range,
    RrrBitVector,
    SingleValue
}