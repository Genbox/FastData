using System.Runtime.InteropServices;

namespace Genbox.FastData.Generators.Contexts.Misc;

[StructLayout(LayoutKind.Auto)]
public readonly record struct HashSetBucket(int StartIndex, int EndIndex);