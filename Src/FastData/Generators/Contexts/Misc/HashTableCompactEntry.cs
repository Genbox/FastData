using System.Runtime.InteropServices;

namespace Genbox.FastData.Generators.Contexts.Misc;

/// <summary>Represents a compact hash-table entry.</summary>
/// <param name="Hash">The hash code for the key.</param>
/// <param name="Key">The stored key.</param>
[StructLayout(LayoutKind.Auto)]
public record struct HashTableCompactEntry<TKey>(ulong Hash, TKey Key);