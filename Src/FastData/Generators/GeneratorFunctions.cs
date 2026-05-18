using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Genbox.FastData.Generators;

public static class GeneratorFunctions
{
    public static uint UnitAt(string str, int offset)
    {
        int index = offset >= 0 ? offset : str.Length + offset;
        Debug.Assert(IsValidIndex(str, index), "UnitAt requires a non-empty string and a valid offset.");
        return str[index];
    }

    public static uint UnitAtAsciiLower(string str, int offset)
    {
        uint unit = UnitAt(str, offset);
        uint candidate = unit | 0x20u;
        return candidate - 'a' <= 'z' - 'a' ? candidate : unit;
    }

    public static int Length(string str) => str.Length;

    public static bool EqualsAt(string str, int offset, string fragment)
    {
        int start = offset >= 0 ? offset : str.Length + offset;
        Debug.Assert(IsValidRange(str, start, fragment.Length), "EqualsAt requires a non-empty string, a valid offset, and a fragment that fits within the string.");
        return string.CompareOrdinal(str, start, fragment, 0, fragment.Length) == 0;
    }

    public static bool EqualsAtAsciiLower(string str, int offset, string fragment)
    {
        int start = offset >= 0 ? offset : str.Length + offset;
        Debug.Assert(IsValidRange(str, start, fragment.Length), "EqualsAtAsciiLower requires a non-empty string, a valid offset, and a fragment that fits within the string.");

        for (int i = 0; i < fragment.Length; i++)
        {
            uint left = str[start + i];
            uint right = fragment[i];

            uint leftCandidate = left | 0x20u;
            uint rightCandidate = right | 0x20u;

            left = leftCandidate - 'a' <= 'z' - 'a' ? leftCandidate : left;
            right = rightCandidate - 'a' <= 'z' - 'a' ? rightCandidate : right;

            if (left != right)
                return false;
        }

        return true;
    }

    public static uint ReadU8(byte[] ptr, int offset) => Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset);
    public static uint ReadU16(byte[] ptr, int offset) => Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset));
    public static uint ReadU32(byte[] ptr, int offset) => Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset));
    public static ulong ReadU64(byte[] ptr, int offset) => Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset));

    private static bool IsValidIndex(string str, int index) => str.Length > 0 && (uint)index < (uint)str.Length;
    private static bool IsValidRange(string str, int start, int length) => str.Length > 0 && (uint)start <= (uint)str.Length && length <= str.Length - start;
}