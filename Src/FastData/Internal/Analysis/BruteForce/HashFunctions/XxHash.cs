using System.Runtime.CompilerServices;
using static Genbox.FastData.Internal.Compat.BitOperations;

namespace Genbox.FastData.Internal.Analysis.BruteForce.HashFunctions;

public static class XxHash
{
    private const ulong PRIME64_1 = 11400714785074694791UL;
    private const ulong PRIME64_2 = 14029467366897019727UL;
    private const ulong PRIME64_3 = 1609587929392839161UL;
    private const ulong PRIME64_4 = 9650029242287828579UL;
    private const ulong PRIME64_5 = 2870177450012600261UL;

    public static unsafe uint ComputeHash(ReadOnlySpan<char> s)
    {
        int length = s.Length;
        fixed (char* src = s)
        {
            ulong hash1 = PRIME64_5 + (ulong)length;

            ulong* ptrUInt64 = (ulong*)src;
            while (length >= 4)
            {
                hash1 ^= Round(0, ptrUInt64[0]);
                hash1 = (RotateLeft(hash1, 27) * PRIME64_1) + PRIME64_4;
                ptrUInt64++;
                length -= 4;
            }

            char* ptrChar = (char*)ptrUInt64;
            while (length-- > 0)
            {
                hash1 ^= ptrChar[0] * PRIME64_5;
                hash1 = RotateLeft(hash1, 11) * PRIME64_1;
            }

            hash1 ^= hash1 >> 33;
            hash1 *= PRIME64_2;
            hash1 ^= hash1 >> 29;
            hash1 *= PRIME64_3;
            hash1 ^= hash1 >> 32;
            return (uint)hash1;
        }
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