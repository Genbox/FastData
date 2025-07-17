using System.Runtime.InteropServices;

namespace Genbox.FastData.Generators.Contexts.Misc;

[StructLayout(LayoutKind.Auto)]
public record struct HashTableEntry<TKey>(ulong Hash, int Next, TKey Key);