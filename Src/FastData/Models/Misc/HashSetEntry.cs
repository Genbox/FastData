using System.Runtime.InteropServices;

namespace Genbox.FastData.Models.Misc;

[StructLayout(LayoutKind.Auto)]
public record struct HashSetEntry(uint Hash, int Next, object Value);