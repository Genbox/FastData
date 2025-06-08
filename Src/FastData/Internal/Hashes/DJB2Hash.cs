using System.Runtime.CompilerServices;

namespace Genbox.FastData.Internal.Hashes;

internal static class DJB2Hash
{
    internal static ulong ComputeHash(ref byte ptr, int length, uint seed = 352654597U)
    {
        unchecked
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
}