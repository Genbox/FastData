using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Genbox.FastData.Generators;

public static class StringFunctions
{
    public static byte ReadU8(byte[] ptr, int offset) => Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset);
    public static ushort ReadU16(byte[] ptr, int offset) => Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset));
    public static uint ReadU32(byte[] ptr, int offset) => Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset));
    public static ulong ReadU64(byte[] ptr, int offset) => Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset));
    public static char GetCharAt(string str, int index) => str[index];
    public static char GetCharAtLower(string str, int index) => char.ToLowerInvariant(str[index]);
    public static char GetCharFromEnd(string str, int fromEnd) => str[str.Length - 1 - fromEnd];
    public static char GetCharFromEndLower(string str, int fromEnd) => char.ToLowerInvariant(str[str.Length - 1 - fromEnd]);
    public static int GetLength(string str) => str.Length;
    public static bool StartsWith(string prefix, string str) => str.StartsWith(prefix, StringComparison.Ordinal);
    public static bool StartsWithIgnoreCase(string prefix, string str) => str.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    public static bool EndsWith(string prefix, string str) => str.EndsWith(prefix, StringComparison.Ordinal);
    public static bool EndsWithIgnoreCase(string prefix, string str) => str.EndsWith(prefix, StringComparison.OrdinalIgnoreCase);
}