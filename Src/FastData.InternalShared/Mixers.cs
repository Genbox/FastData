using System.Runtime.CompilerServices;

namespace Genbox.FastData.InternalShared;

public static class Mixers
{
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