namespace Genbox.FastData.Enums;

public enum DataStructure : byte
{
    // We always need something to represent a single value
    SingleValue,

    // O(n) data structures
    Array,
    Conditional,
    Switch,

    // O(log(n)) data structures
    BinarySearch,
    EytzingerSearch,

    // O(1) data structures
    MinimalPerfectHash,
    HashSetLinear,
    HashSetChain,

    // Edge-cases
    KeyLength,
    UniqueKeyLength,
    UniqueKeyLengthSwitch,
}