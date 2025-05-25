namespace Genbox.FastData.Enums;

public enum StructureType : byte
{
    Auto = 0,

    // O(n) data structures
    Array,
    Conditional,

    // O(log(n)) data structures
    BinarySearch,

    // O(1) data structures
    HashSet
}