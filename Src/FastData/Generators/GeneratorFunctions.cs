using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Genbox.FastData.Generators;

/// <summary>Provides runtime equivalents of helper functions emitted by code generators.</summary>
public static class GeneratorFunctions
{
    /// <summary>Reads a UTF-16 code unit at an absolute or end-relative offset.</summary>
    /// <param name="str">The input string.</param>
    /// <param name="offset">A non-negative start offset or negative end-relative offset.</param>
    /// <returns>The code unit.</returns>
    public static uint UnitAt(string str, int offset)
    {
        if (str == null)
            throw new ArgumentNullException(nameof(str));

        int index = offset >= 0 ? offset : str.Length + offset;
        Debug.Assert(IsValidIndex(str, index), "UnitAt requires a non-empty string and a valid offset.");
        return str[index];
    }

    /// <summary>Reads a code unit and normalizes an ASCII letter to lowercase.</summary>
    /// <param name="str">The input string.</param>
    /// <param name="offset">A non-negative start offset or negative end-relative offset.</param>
    /// <returns>The normalized code unit.</returns>
    public static uint UnitAtAsciiLower(string str, int offset)
    {
        uint unit = UnitAt(str, offset);
        uint candidate = unit | 0x20u;
        return unchecked(candidate - 'a') <= 'z' - 'a' ? candidate : unit;
    }

    /// <summary>Gets the UTF-16 length of a string.</summary>
    /// <param name="str">The input string.</param>
    /// <returns>The string length.</returns>
    public static int Length(string str)
    {
        if (str == null)
            throw new ArgumentNullException(nameof(str));

        return str.Length;
    }

    /// <summary>Compares a fragment at an absolute or end-relative offset.</summary>
    /// <param name="str">The input string.</param>
    /// <param name="offset">A non-negative start offset or a negative end-relative offset.</param>
    /// <param name="fragment">The fragment to compare.</param>
    /// <returns><see langword="true" /> when the fragment matches.</returns>
    public static bool EqualsAt(string str, int offset, string fragment)
    {
        if (str == null)
            throw new ArgumentNullException(nameof(str));
        if (fragment == null)
            throw new ArgumentNullException(nameof(fragment));

        int start = offset >= 0 ? offset : str.Length - fragment.Length;
        Debug.Assert(IsValidRange(str, start, fragment.Length), "EqualsAt requires a non-empty string, a valid offset, and a fragment that fits within the string.");
        return string.CompareOrdinal(str, start, fragment, 0, fragment.Length) == 0;
    }

    /// <summary>Compares a fragment using ASCII case folding.</summary>
    /// <param name="str">The input string.</param>
    /// <param name="offset">A non-negative start offset or a negative end-relative offset.</param>
    /// <param name="fragment">The fragment to compare.</param>
    /// <returns><see langword="true" /> when the fragment matches.</returns>
    public static bool EqualsAtAsciiLower(string str, int offset, string fragment)
    {
        if (str == null)
            throw new ArgumentNullException(nameof(str));
        if (fragment == null)
            throw new ArgumentNullException(nameof(fragment));

        int start = offset >= 0 ? offset : str.Length - fragment.Length;
        Debug.Assert(IsValidRange(str, start, fragment.Length), "EqualsAtAsciiLower requires a non-empty string, a valid offset, and a fragment that fits within the string.");

        for (int i = 0; i < fragment.Length; i++)
        {
            uint left = str[start + i];
            uint right = fragment[i];

            uint leftCandidate = left | 0x20u;
            uint rightCandidate = right | 0x20u;

            left = unchecked(leftCandidate - 'a') <= 'z' - 'a' ? leftCandidate : left;
            right = unchecked(rightCandidate - 'a') <= 'z' - 'a' ? rightCandidate : right;

            if (left != right)
                return false;
        }

        return true;
    }

    /// <summary>Determines whether every code unit is in the ASCII range.</summary>
    /// <param name="value">The string to inspect.</param>
    /// <returns><see langword="true" /> when the string contains only ASCII characters.</returns>
    public static bool IsAsciiOnly(string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        int offset = 0;

        while (value.Length - offset >= 4)
        {
            uint left = value[offset] | ((uint)value[offset + 1] << 16);
            uint right = value[offset + 2] | ((uint)value[offset + 3] << 16);

            if (((left | right) & ~0x007F_007Fu) != 0)
                return false;

            offset += 4;
        }

        while (offset < value.Length)
        {
            if (value[offset++] >= 0x80)
                return false;
        }

        return true;
    }

    /// <summary>Reads an unsigned byte at an offset.</summary>
    /// <param name="ptr">The source buffer.</param>
    /// <param name="offset">The byte offset.</param>
    /// <returns>The value.</returns>
    public static uint ReadU8(byte[] ptr, int offset) => Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset);

    /// <summary>Reads an unaligned unsigned 16-bit value at an offset.</summary>
    /// <param name="ptr">The source buffer.</param>
    /// <param name="offset">The byte offset.</param>
    /// <returns>The value.</returns>
    public static uint ReadU16(byte[] ptr, int offset) => Unsafe.ReadUnaligned<ushort>(ref Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset));

    /// <summary>Reads an unaligned unsigned 32-bit value at an offset.</summary>
    /// <param name="ptr">The source buffer.</param>
    /// <param name="offset">The byte offset.</param>
    /// <returns>The value.</returns>
    public static uint ReadU32(byte[] ptr, int offset) => Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset));

    /// <summary>Reads an unaligned unsigned 64-bit value at an offset.</summary>
    /// <param name="ptr">The source buffer.</param>
    /// <param name="offset">The byte offset.</param>
    /// <returns>The value.</returns>
    public static ulong ReadU64(byte[] ptr, int offset) => Unsafe.ReadUnaligned<ulong>(ref Unsafe.Add(ref MemoryMarshal.GetReference(ptr.AsSpan()), offset));

    private static bool IsValidIndex(string str, int index) => str.Length > 0 && (uint)index < (uint)str.Length;
    private static bool IsValidRange(string str, int start, int length) => str.Length > 0 && (uint)start <= (uint)str.Length && length <= str.Length - start;
}