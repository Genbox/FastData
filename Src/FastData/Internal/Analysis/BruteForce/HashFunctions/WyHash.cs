using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Genbox.FastData.Internal.Analysis.BruteForce.HashFunctions.Shared;

namespace Genbox.FastData.Internal.Analysis.BruteForce.HashFunctions;

public static class WyHash
{
    private const ulong PRIME64_1 = 0xa0761d6478bd642ful;
    private const ulong PRIME64_2 = 0xe7037ed1a0b428dbul;

    public static uint ComputeHash(ReadOnlySpan<char> s)
    {
        return ComputeHash(ref MemoryMarshal.GetReference(s), s.Length);
    }

    public static uint ComputeHash(ref char ptr, int length)
    {
        ulong seed = PRIME64_1;
        ulong a, b;

        ref uint ptr32 = ref Unsafe.As<char, uint>(ref ptr);
        if (length <= 8)
        {
            if (length >= 2)
            {
                a = ((ulong)ptr32 << 32) | Unsafe.Add(ref ptr32, (length >> 3) << 2);
                b = ((ulong)Unsafe.Add(ref ptr32, length - 4) << 32) | Unsafe.Add(ref ptr32, length - 4 - ((length >> 3) << 2));
            }
            else if (length > 0)
            {
                a = ((ulong)ptr << 16) | ((ulong)Unsafe.Add(ref ptr, length >> 1) << 8) | Unsafe.Add(ref ptr, length - 1);
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
            int rem = length;
            ref ulong ptr64 = ref Unsafe.As<char, ulong>(ref ptr);

            while (rem > 8)
            {
                seed = Mix(ptr64 ^ PRIME64_2, Unsafe.Add(ref ptr64, 1) ^ seed);
                rem -= 8;
                ptr64 = ref Unsafe.Add(ref ptr64, 2);
            }

            a = Unsafe.Add(ref ptr64, rem - 8);
            b = Unsafe.Add(ref ptr64, rem - 4);
        }

        return (uint)Mix(PRIME64_2 ^ (ulong)length, Mix(a ^ PRIME64_2, b ^ seed));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong Mix(ulong A, ulong B)
    {
        ulong high = BigMul(A, B, out ulong low);
        return low ^ high;
    }
}