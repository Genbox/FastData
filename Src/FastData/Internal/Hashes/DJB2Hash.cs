using System.Runtime.CompilerServices;

namespace Genbox.FastData.Internal.Hashes;

internal static class DJB2Hash
{
    internal static uint ComputeHash(ref char ptr, int length, uint seed = 352654597U)
    {
        uint hash = seed;

        while (length-- > 0)
        {
            hash = (((hash << 5) | (hash >> 27)) + hash) ^ ptr;
            ptr = ref Unsafe.Add(ref ptr, 1);
        }

        return seed + (hash * 1566083941);
    }

    internal static ulong ComputeHash64(ref char ptr, int length, uint seed = 352654597U)
    {
        ulong hash = seed;

        while (length-- > 0)
        {
            hash = (((hash << 5) | (hash >> 27)) + hash) ^ ptr;
            ptr = ref Unsafe.Add(ref ptr, 1);
        }

        return seed + (hash * 1566083941);
    }

    internal static uint ComputeHash(ref byte ptr, int length, uint seed = 352654597U)
    {
        uint hash = seed;

        while (length-- > 0)
        {
            hash = (((hash << 5) | (hash >> 27)) + hash) ^ ptr;
            ptr = ref Unsafe.Add(ref ptr, 1);
        }

        return seed + (hash * 1566083941);
    }

    internal static ulong ComputeHash64(ref byte ptr, int length, uint seed = 352654597U)
    {
        ulong hash = seed;

        while (length-- > 0)
        {
            hash = (((hash << 5) | (hash >> 27)) + hash) ^ ptr;
            ptr = ref Unsafe.Add(ref ptr, 1);
        }

        return seed + (hash * 1566083941);
    }
}