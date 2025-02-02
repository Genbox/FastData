using System.Runtime.CompilerServices;
using static Genbox.FastData.Internal.Analysis.BruteForce.HashFunctions.Shared;
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

        fixed (char* cPtr = s)
        {
            byte* data = (byte*)cPtr;

            ulong h64;
            if (length >= 32)
            {
                byte* bEnd = data + length;
                byte* limit = bEnd - 31;

                ulong v1 = unchecked(PRIME64_1 + PRIME64_2);
                ulong v2 = PRIME64_2;
                ulong v3 = 0;
                ulong v4 = PRIME64_1;

                do
                {
                    v1 = Round(v1, Read64(data));
                    data += 8;
                    v2 = Round(v2, Read64(data));
                    data += 8;
                    v3 = Round(v3, Read64(data));
                    data += 8;
                    v4 = Round(v4, Read64(data));
                    data += 8;
                } while (data < limit);

                h64 = RotateLeft(v1, 1) + RotateLeft(v2, 7) + RotateLeft(v3, 12) + RotateLeft(v4, 18);
                h64 = MergeRound(h64, v1);
                h64 = MergeRound(h64, v2);
                h64 = MergeRound(h64, v3);
                h64 = MergeRound(h64, v4);
            }
            else
                h64 = PRIME64_5;

            h64 += (uint)length;

            length &= 31;
            while (length >= 8)
            {
                ulong k1 = Round(0, Read64(data));
                data += 8;
                h64 ^= k1;
                h64 = (RotateLeft(h64, 27) * PRIME64_1) + PRIME64_4;
                length -= 8;
            }

            if (length >= 4)
            {
                h64 ^= Read32(data) * PRIME64_1;
                data += 4;
                h64 = (RotateLeft(h64, 23) * PRIME64_2) + PRIME64_3;
                length -= 4;
            }

            while (length > 0)
            {
                h64 ^= Read8(data) * PRIME64_5;
                data++;
                h64 = RotateLeft(h64, 11) * PRIME64_1;
                length--;
            }

            h64 ^= h64 >> 33;
            h64 *= PRIME64_2;
            h64 ^= h64 >> 29;
            h64 *= PRIME64_3;
            h64 ^= h64 >> 32;
            return (uint)h64;
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong MergeRound(ulong acc, ulong val)
    {
        val = Round(0, val);
        acc ^= val;
        acc = (acc * PRIME64_1) + PRIME64_4;
        return acc;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong YC_xmxmx_XXH2_64(ulong h)
    {
        h ^= h >> 33;
        h *= 0xC2B2AE3D27D4EB4F;
        h ^= h >> 29;
        h *= 0x165667B19E3779F9;
        h ^= h >> 32;
        return h;
    }
}