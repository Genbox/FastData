using System.Runtime.InteropServices;

namespace Genbox.FastData.Generators.Contexts.Misc;

[StructLayout(LayoutKind.Auto)]
public record struct HashTableEntry<T>(ulong Hash, int Next, T Value);