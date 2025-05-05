using System.Runtime.CompilerServices;

namespace Genbox.FastData.InternalShared;

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
}