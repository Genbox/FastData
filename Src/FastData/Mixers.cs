using System.Runtime.CompilerServices;

namespace Genbox.FastData;

public static class Mixers
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Murmur_32(uint h)
    {
        unchecked
        {
            h ^= h >> 16;
            h *= 0x85EBCA6BU;
            h ^= h >> 13;
            h *= 0xC2B2AE35U;
            h ^= h >> 16;
            return h;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong Murmur_64(ulong h)
    {
        unchecked
        {
            h ^= h >> 33;
            h *= 0xFF51AFD7ED558CCD;
            h ^= h >> 33;
            h *= 0xC4CEB9FE1A85EC53;
            h ^= h >> 33;
            return h;
        }
    }
}