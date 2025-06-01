using System.Diagnostics.CodeAnalysis;

namespace Genbox.FastData.Internal.Helpers;

internal static class PerfectHashHelper
{
    [SuppressMessage("Major Bug", "S1751:Loops with at most one iteration should be refactored")]
    internal static uint Generate(ulong[] hashCodes, Func<ulong, ulong, ulong> mixer, uint maxAttempts = uint.MaxValue, uint length = 0)
    {
        //Length = 0 means minimal perfect hash function
        if (length == 0)
            length = (uint)hashCodes.Length;

        if (length == 1)
            return 1;

        uint seed;
        SwitchArray arr = new SwitchArray(length);

        //Hash each candidate. Exit when the first duplicate is detected, or when we run out of candidates to test.
        for (seed = 1; seed < maxAttempts; seed++)
        {
            arr.Clear();

            for (int i = 0; i < hashCodes.Length; i++)
            {
                uint offset = (uint)(mixer(hashCodes[i], seed) % length);

                //If this offset is already set we can early exit
                if (arr[offset])
                    goto TryAgain;

                arr[offset] = true;
            }

            return seed;
            TryAgain: ;
        }

        return 0;
    }
}