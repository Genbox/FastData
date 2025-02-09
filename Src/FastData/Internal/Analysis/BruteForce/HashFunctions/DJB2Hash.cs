using static Genbox.FastData.Internal.Compat.BitOperations;

namespace Genbox.FastData.Internal.Analysis.BruteForce.HashFunctions;

public static class DJB2Hash
{
    private const uint Seed = (5381 << 16) + 5381;
    private const uint Factor = 1_566_083_941;

    public static unsafe uint ComputeHash(ReadOnlySpan<char> s)
    {
        int length = s.Length;
        fixed (char* src = s)
        {
            uint hash1 = Seed;
            uint hash2 = Seed;

            uint* ptrUInt32 = (uint*)src;
            while (length >= 4)
            {
                hash1 = (RotateLeft(hash1, 5) + hash1) ^ ptrUInt32[0];
                hash2 = (RotateLeft(hash2, 5) + hash2) ^ ptrUInt32[1];
                ptrUInt32 += 2;
                length -= 4;
            }

            char* ptrChar = (char*)ptrUInt32;
            while (length-- > 0)
                hash2 = (RotateLeft(hash2, 5) + hash2) ^ *ptrChar++;

            return hash1 + (hash2 * Factor);
        }
    }
}