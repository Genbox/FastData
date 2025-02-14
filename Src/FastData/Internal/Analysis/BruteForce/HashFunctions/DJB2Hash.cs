using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Genbox.FastData.Internal.Compat.BitOperations;

namespace Genbox.FastData.Internal.Analysis.BruteForce.HashFunctions;

public static class DJB2Hash
{
    private const uint Seed = (5381 << 16) + 5381;
    private const uint Factor = 1_566_083_941;

    public static uint ComputeHash(ReadOnlySpan<char> span)
    {
        ref char ptr = ref MemoryMarshal.GetReference(span);
        return ComputeHash(ref ptr, span.Length);
    }

    public static uint ComputeHash(ref char ptr, int length)
    {
        uint hash1, hash2;
        switch (length)
        {
            case 1:
                hash2 = (RotateLeft(Seed, 5) + Seed) ^ ptr;
                return Seed + (hash2 * Factor);

            case 2:
                hash2 = (RotateLeft(Seed, 5) + Seed) ^ ptr;
                hash2 = (RotateLeft(hash2, 5) + hash2) ^ Unsafe.Add(ref ptr, 1);
                return Seed + (hash2 * Factor);

            case 3:
                hash2 = (RotateLeft(Seed, 5) + Seed) ^ ptr;
                hash2 = (RotateLeft(hash2, 5) + hash2) ^ Unsafe.Add(ref ptr, 1);
                hash2 = (RotateLeft(hash2, 5) + hash2) ^ Unsafe.Add(ref ptr, 2);
                return Seed + (hash2 * Factor);

            case 4:
            {
                ref uint ptr32 = ref Unsafe.As<char, uint>(ref ptr);

                hash1 = (RotateLeft(Seed, 5) + Seed) ^ ptr32;
                hash2 = (RotateLeft(Seed, 5) + Seed) ^ Unsafe.Add(ref ptr32, 1);
                return hash1 + (hash2 * Factor);
            }
            default:
            {
                hash1 = Seed;
                hash2 = hash1;

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