using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Genbox.FastData.Internal.Compat.BitOperations;

namespace Genbox.FastData.Internal.Analysis.BruteForce.HashFunctions;

public static class XxHash
{
    private const ulong PRIME64_1 = 11400714785074694791UL;
    private const ulong PRIME64_2 = 14029467366897019727UL;
    private const ulong PRIME64_3 = 1609587929392839161UL;
    private const ulong PRIME64_4 = 9650029242287828579UL;
    private const ulong PRIME64_5 = 2870177450012600261UL;

    public static uint ComputeHash(ReadOnlySpan<char> s)
    {
        return ComputeHash(ref MemoryMarshal.GetReference(s), s.Length);
    }

    public static uint ComputeHash(ref char ptr, int length)
    {
        ulong hash1 = PRIME64_5 + (ulong)length;

        ref ulong ptr64 = ref Unsafe.As<char, ulong>(ref ptr);
        while (length >= 4)
        {
            hash1 ^= Round(0, ptr64);
            hash1 = (RotateLeft(hash1, 27) * PRIME64_1) + PRIME64_4;
            ptr64 = ref Unsafe.Add(ref ptr64, 1);
            length -= 4;
        }

        ref ushort ptr16 = ref Unsafe.As<ulong, ushort>(ref ptr64);
        while (length-- > 0)
        {
            hash1 ^= ptr16 * PRIME64_5;
            hash1 = RotateLeft(hash1, 11) * PRIME64_1;
            ptr16 = ref Unsafe.Add(ref ptr16, 1);
        }

        hash1 ^= hash1 >> 33;
        hash1 *= PRIME64_2;
        hash1 ^= hash1 >> 29;
        hash1 *= PRIME64_3;
        hash1 ^= hash1 >> 32;
        return unchecked((uint)hash1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Round(ulong acc, ulong input)
    {
        acc += input * PRIME64_2;
        acc = RotateLeft(acc, 31);
        acc *= PRIME64_1;
        return acc;
    }
}