using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Genbox.FastData.Internal.Compat.BitOperations;

namespace Genbox.FastData.Helpers;

/// <summary>
/// This helper ensures that we get consistent hashes between compile-time and runtime
/// </summary>
public static class HashHelper
{
    private const uint Accumulator = (5381 << 16) + 5381;
    private const uint Factor = 1_566_083_941;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint HashObject(object data)
    {
        //We don't want randomized hash codes, so we handle string as a special case
        if (data is string str)
            return HashString(str.AsSpan());

        return (uint)data.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint HashObjectSeed(object data, uint seed, bool strongMixing)
    {
        //We don't want randomized hash codes, so we handle string as a special case
        if (data is string str)
            return HashStringSeed(str.AsSpan(), seed);

        uint code = (uint)data.GetHashCode();
        return strongMixing ? Mix(code + seed) : code + seed;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe uint HashString(ReadOnlySpan<char> data)
    {
        int length = data.Length;
        fixed (char* src = &MemoryMarshal.GetReference(data))
        {
            uint hash1, hash2;
            switch (length)
            {
                case 1:
                    hash2 = (RotateLeft(Accumulator, 5) + Accumulator) ^ src[0];
                    return Accumulator + (hash2 * Factor);

                case 2:
                    hash2 = (RotateLeft(Accumulator, 5) + Accumulator) ^ src[0];
                    hash2 = (RotateLeft(hash2, 5) + hash2) ^ src[1];
                    return Accumulator + (hash2 * Factor);

                case 3:
                    hash2 = (RotateLeft(Accumulator, 5) + Accumulator) ^ src[0];
                    hash2 = (RotateLeft(hash2, 5) + hash2) ^ src[1];
                    hash2 = (RotateLeft(hash2, 5) + hash2) ^ src[2];
                    return Accumulator + (hash2 * Factor);

                case 4:
                    hash1 = (RotateLeft(Accumulator, 5) + Accumulator) ^ ((uint*)src)[0];
                    hash2 = (RotateLeft(Accumulator, 5) + Accumulator) ^ ((uint*)src)[1];
                    return hash1 + (hash2 * Factor);

                default:
                    hash1 = Accumulator;
                    hash2 = hash1;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static unsafe uint HashStringSeed(ReadOnlySpan<char> data, uint seed)
    {
        int length = data.Length;
        fixed (char* src = &MemoryMarshal.GetReference(data))
        {
            uint hash1, hash2;
            switch (length)
            {
                case 1:
                    hash2 = (RotateLeft(Accumulator + seed, 5) + Accumulator + seed) ^ src[0];
                    return Accumulator + seed + (hash2 * Factor);

                case 2:
                    hash2 = (RotateLeft(Accumulator + seed, 5) + Accumulator + seed) ^ src[0];
                    hash2 = (RotateLeft(hash2, 5) + hash2) ^ src[1];
                    return Accumulator + seed + (hash2 * Factor);

                case 3:
                    hash2 = (RotateLeft(Accumulator + seed, 5) + Accumulator + seed) ^ src[0];
                    hash2 = (RotateLeft(hash2, 5) + hash2) ^ src[1];
                    hash2 = (RotateLeft(hash2, 5) + hash2) ^ src[2];
                    return Accumulator + seed + (hash2 * Factor);

                case 4:
                    hash1 = (RotateLeft(Accumulator + seed, 5) + Accumulator + seed) ^ ((uint*)src)[0];
                    hash2 = (RotateLeft(Accumulator + seed, 5) + Accumulator + seed) ^ ((uint*)src)[1];
                    return hash1 + (hash2 * Factor);

                default:
                    hash1 = Accumulator + seed;
                    hash2 = hash1;

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Mix(uint h)
    {
        h ^= h >> 16;
        h *= 0x85ebca6b;
        h ^= h >> 13;
        h *= 0xc2b2ae35;
        h ^= h >> 16;
        return h;
    }
}