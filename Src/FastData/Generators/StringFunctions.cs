using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Genbox.FastData.Generators;

public static class StringFunctions
{
    public static byte ReadU8(byte[] ptr, int offset) => Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset);
    public static ushort ReadU16(byte[] ptr, int offset) => Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset));
    public static uint ReadU32(byte[] ptr, int offset) => Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset));
    public static ulong ReadU64(byte[] ptr, int offset) => Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset));
    public static char GetCharAt(string str, int offset) => offset >= 0 ? str[offset] : str[str.Length + offset];
    public static char GetCharAtLower(string str, int offset) => char.ToLowerInvariant(offset >= 0 ? str[offset] : str[str.Length + offset]);
    public static int GetLength(string str) => str.Length;
    public static bool StringAt(string fragment, int offset, string str) { int start = offset >= 0 ? offset : str.Length + offset; return string.Compare(str, start, fragment, 0, fragment.Length, StringComparison.Ordinal) == 0; }
    public static bool StringAtIgnoreCase(string fragment, int offset, string str) { int start = offset >= 0 ? offset : str.Length + offset; return string.Compare(str, start, fragment, 0, fragment.Length, StringComparison.OrdinalIgnoreCase) == 0; }
}