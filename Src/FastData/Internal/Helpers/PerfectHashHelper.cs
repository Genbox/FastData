using Genbox.FastData.Helpers;
using Genbox.FastData.Specs;

namespace Genbox.FastData.Internal.Helpers;

internal static class PerfectHashHelper
{
    internal static IEnumerable<uint> Generate<T>(T[] data, HashFunc hashFunc, uint maxCandidates = uint.MaxValue, uint maxAttempts = uint.MaxValue, int length = 0, Func<bool>? condition = null)
    {
        if (length == 0)
            length = data.Length;

        if (length == 1)
        {
            yield return 1;
            yield break;
        }

        //If there are duplicate hash codes, we might as well early exit as there exist no solution
        HashSet<uint> codes = new HashSet<uint>();
        foreach (T item in data)
        {
            if (!codes.Add(hashFunc(item)))
                yield break;
        }

        bool[] bArray = new bool[length];
        ulong fastMod = MathHelper.GetFastModMultiplier((uint)length);
        int numFound = 0;
        uint seed;

        //Hash each candidate. Exit when the first duplicate is detected, or when we run out of candidates to test.
        for (seed = 1; seed < uint.MaxValue - 1; seed++)
        {
            //Clear the array for next use
            Array.Clear(bArray, 0, bArray.Length);

            for (int k = 0; k < data.Length; k++)
            {
                uint offset = MathHelper.FastMod(hashFunc(data[k], seed), (uint)length, fastMod);

                //If this offset is already set we can early exit
                if (bArray[offset])
                    goto NotFound;

                bArray[offset] = true;
            }

            numFound++;
            yield return seed;

            if (numFound == maxCandidates)
                yield break;

            NotFound:

            if (maxAttempts-- == 0)
                yield break;

            if (condition != null && condition())
                yield break;
        }
    }

    internal static bool Validate<T>(T[] data, uint seed, Func<T, uint, ulong> hashFunc, out byte[] offsets, uint length = 0)
    {
        if (length == 0)
            length = (uint)data.Length;

        bool[] bArray = new bool[length];
        offsets = new byte[length];
        ulong fastMod = MathHelper.GetFastModMultiplier(length);
        for (uint i = 0; i < data.Length; i++)
        {
            uint offset = MathHelper.FastMod((uint)hashFunc(data[i], seed), length, fastMod);
            offsets[i] = (byte)offset;

            if (bArray[offset])
                return false;
        }

        return true;
    }
}