using Genbox.FastData.Helpers;

namespace Genbox.FastData.Internal.Helpers;

internal static class PerfectHashHelper
{
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

            TryAgain: ;
        }

        return seed == maxAttempts ? 0 : seed;
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
                Array.Clear(_data, 0, _data.Length);
        }
    }

    internal static bool Validate(uint[] hashCodes, uint seed, Func<uint, uint, uint> mixer, out byte[] offsets, uint length = 0)
    {
        if (length == 0)
            length = (uint)hashCodes.Length;

        bool[] bArray = new bool[length];
        offsets = new byte[length];
        ulong fastMod = MathHelper.GetFastModMultiplier(length);

        for (uint i = 0; i < hashCodes.Length; i++)
        {
            uint offset = MathHelper.FastMod(mixer(hashCodes[i], seed), length, fastMod);
            offsets[i] = (byte)offset;

            if (bArray[offset])
                return false;
        }

        return true;
    }
}