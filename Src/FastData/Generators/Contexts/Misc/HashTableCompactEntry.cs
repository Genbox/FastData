using System.Runtime.InteropServices;

namespace Genbox.FastData.Generators.Contexts.Misc;

[StructLayout(LayoutKind.Auto)]
public record struct HashTableCompactEntry<TKey>(ulong Hash, TKey Key);
