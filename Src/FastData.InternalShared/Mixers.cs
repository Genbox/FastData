using System.Runtime.CompilerServices;

namespace Genbox.FastData.InternalShared;

public static class Mixers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Murmur_32_Seed(uint h, uint seed)
    {
        unchecked
        {
            h += seed;
            h ^= h >> 16;
            h *= 0x85EBCA6BU;
            h ^= h >> 13;
            h *= 0xC2B2AE35U;
            h ^= h >> 16;
            return h;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint XXH2_32_Seed(uint h, uint seed)
    {
        unchecked
        {
            h += seed;
            h ^= h >> 15;
            h *= 0x85EBCA77U;
            h ^= h >> 13;
            h *= 0xC2B2AE3DU;
            h ^= h >> 16;
            return h;
        }
    }
}