using System.Runtime.InteropServices;

namespace Genbox.FastData.Generators.Contexts.Misc;

/// <summary>Represents an entry in a chained hash table.</summary>
/// <param name="Hash">The hash code for the key.</param>
/// <param name="Next">The index of the next entry in the bucket chain.</param>
/// <param name="Key">The stored key.</param>
[StructLayout(LayoutKind.Auto)]
public record struct HashTableEntry<TKey>(ulong Hash, int Next, TKey Key);