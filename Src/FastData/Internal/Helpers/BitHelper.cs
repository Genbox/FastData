using System.Runtime.CompilerServices;

namespace Genbox.FastData.Internal.Helpers;

public static class BitHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool AreBitsConsecutive(ulong x)
    {
        if (x == 0)
            return false;

        if (x == ulong.MaxValue)
            return true;

        x /= x & ~(x - 1);
        return (x & (x + 1)) == 0;
    }
}