using System.Runtime.InteropServices;

namespace Genbox.FastData.Contexts.Misc;

[StructLayout(LayoutKind.Auto)]
public record struct HashSetEntry<T>(uint Hash, int Next, T Value);