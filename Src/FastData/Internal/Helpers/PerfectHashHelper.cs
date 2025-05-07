using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Helpers;

namespace Genbox.FastData.Internal.Helpers;

internal static class PerfectHashHelper
{
    [SuppressMessage("Major Bug", "S1751:Loops with at most one iteration should be refactored")]
    internal static uint Generate(uint[] hashCodes, Func<uint, uint, uint> mixer, uint maxAttempts = uint.MaxValue, uint length = 0)
    {
        //Length = 0 means minimal perfect hash function
        if (length == 0)
            length = (uint)hashCodes.Length;

        if (length == 1)
            return 1;

        uint seed;
        SwitchArray arr = new SwitchArray(length);
        ulong fastMod = MathHelper.GetFastModMultiplier(length);

        //Hash each candidate. Exit when the first duplicate is detected, or when we run out of candidates to test.
        for (seed = 1; seed < maxAttempts; seed++)
        {
            arr.Clear();

            for (int i = 0; i < hashCodes.Length; i++)
            {
                uint offset = MathHelper.FastMod(mixer(hashCodes[i], seed), length, fastMod);

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

    private sealed class SwitchArray(uint capacity)
    {
        private readonly uint[] _data = new uint[capacity];
        private uint _counter;

        public bool this[uint index]
        {
            get => _data[index] == _counter;
            set => _data[index] = _counter;
        }

        public void Clear()
        {
            _counter++;

            if (_counter == uint.MaxValue)
            {
                Array.Clear(_data, 0, _data.Length);
                _counter = 0;
            }
        }
    }
}