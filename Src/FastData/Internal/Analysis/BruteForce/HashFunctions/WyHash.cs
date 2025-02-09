using System.Runtime.CompilerServices;
using static Genbox.FastData.Internal.Analysis.BruteForce.HashFunctions.Shared;

namespace Genbox.FastData.Internal.Analysis.BruteForce.HashFunctions;

public static class WyHash
{
    private const ulong PRIME64_1 = 0xa0761d6478bd642ful;
    private const ulong PRIME64_2 = 0xe7037ed1a0b428dbul;

    public static unsafe uint ComputeHash(ReadOnlySpan<char> s)
    {
        ulong seed = PRIME64_1;
        int len = s.Length;

        fixed (char* cPtr = s)
        {
            ulong a, b;

            uint* uPtr32 = (uint*)cPtr;
            if (len <= 16)
            {
                if (len >= 4)
                {
                    a = ((ulong)uPtr32[0] << 32) | uPtr32[(len >> 3) << 2];
                    b = ((ulong)uPtr32[len - 4] << 32) | uPtr32[len - 4 - ((len >> 3) << 2)];
                }
                else if (len > 0)
                {
                    a = ((ulong)cPtr[0] << 16) | ((ulong)cPtr[len >> 1] << 8) | cPtr[len - 1];
                    b = 0;
                }
                else
                {
                    a = 0;
                    b = 0;
                }
            }
            else
            {
                int rem = len;
                uint* uPtr64 = (uint*)cPtr;

                while (rem > 16)
                {
                    seed = Mix(uPtr64[0] ^ PRIME64_2, uPtr64[1] ^ seed);
                    rem -= 16;
                    uPtr64 += 2;
                }

                a = uPtr64[rem - 16];
                b = uPtr64[rem - 8];
            }

            return (uint)Mix(PRIME64_2 ^ (ulong)len, Mix(a ^ PRIME64_2, b ^ seed));
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Mix(ulong A, ulong B)
    {
        ulong high = BigMul(A, B, out ulong low);
        return low ^ high;
    }
}