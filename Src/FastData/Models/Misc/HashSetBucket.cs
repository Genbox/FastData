using System.Runtime.InteropServices;

namespace Genbox.FastData.Models.Misc;

[StructLayout(LayoutKind.Auto)]
public readonly record struct HashSetBucket(int StartIndex, int EndIndex);