namespace Genbox.FastData.SourceGenerator.Attributes;

/// <summary>Specifies the data structure type to generate.</summary>
public enum StructureType : byte
{
    /// <summary>Automatically select the best structure type.</summary>
    Auto = 0,

    /// <summary>Use a simple array structure. Complexity: O(n).</summary>
    Array,

    /// <summary>Use a conditional structure. Complexity: O(n).</summary>
    Conditional,

    /// <summary>Use a binary search structure. Complexity: O(log n).</summary>
    BinarySearch,

    /// <summary>Use a hash set structure. Complexity: O(1).</summary>
    HashTable
}