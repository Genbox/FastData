using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Numerics.BitOperations;

namespace Genbox.FastData.Internal.Analysis.Techniques.BruteForce.HashFunctions;

public static class DJB2Hash
{
    private const uint Seed = (5381 << 16) + 5381;
    private const uint Factor = 0x5D588B65;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ComputeHash(ReadOnlySpan<char> span, uint seed = Seed)
    {
        ref char ptr = ref MemoryMarshal.GetReference(span);
        return ComputeHash(ref ptr, span.Length, seed);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ComputeHash(ref char ptr, int length, uint seed = Seed)
    {
        uint hash1, hash2;
        switch (length)
        {
            case 1:
                hash2 = (RotateLeft(seed, 5) + seed) ^ ptr;
                return seed + (hash2 * Factor);

            case 2:
                hash2 = (RotateLeft(seed, 5) + seed) ^ ptr;
                hash2 = (RotateLeft(hash2, 5) + hash2) ^ Unsafe.Add(ref ptr, 1);
                return seed + (hash2 * Factor);

            case 3:
                hash2 = (RotateLeft(seed, 5) + seed) ^ ptr;
                hash2 = (RotateLeft(hash2, 5) + hash2) ^ Unsafe.Add(ref ptr, 1);
                hash2 = (RotateLeft(hash2, 5) + hash2) ^ Unsafe.Add(ref ptr, 2);
                return seed + (hash2 * Factor);

            case 4:
            {
                ref uint ptr32 = ref Unsafe.As<char, uint>(ref ptr);

                hash1 = (RotateLeft(seed, 5) + seed) ^ ptr32;
                hash2 = (RotateLeft(seed, 5) + seed) ^ Unsafe.Add(ref ptr32, 1);
                return hash1 + (hash2 * Factor);
            }
            default:
            {
                hash1 = seed;
                hash2 = seed;

                ref uint ptr32 = ref Unsafe.As<char, uint>(ref ptr);

                while (length >= 4)
                {
                    hash1 = (RotateLeft(hash1, 5) + hash1) ^ ptr32;
                    hash2 = (RotateLeft(hash2, 5) + hash2) ^ Unsafe.Add(ref ptr32, 1);

                    ptr32 = ref Unsafe.Add(ref ptr32, 2);
                    length -= 4;
                }

                ref char ptrChar = ref Unsafe.As<uint, char>(ref ptr32);
                while (length-- > 0)
                {
                    hash2 = (RotateLeft(hash2, 5) + hash2) ^ ptrChar;
                    ptrChar = ref Unsafe.Add(ref ptrChar, 1);
                }

                return hash1 + (hash2 * Factor);
            }
        }
    }
}