namespace Genbox.FastData.Enums;

public enum DataStructure : byte
{
    // We always need something to represent a single value
    SingleValue,

    // O(n) data structures
    Array,
    Conditional,

    // O(log(n)) data structures
    BinarySearch,
    EytzingerSearch,

    // O(1) data structures
    PerfectHashGPerf,
    PerfectHashBruteForce,
    HashSetChain,
    HashSetLinear,

    // Edge-cases
    KeyLength
}