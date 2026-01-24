using System.Runtime.CompilerServices;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Analysis.Data;

internal sealed class LengthBitArray(int length = 64)
{
    private readonly SwitchingBitSet _tracker = new SwitchingBitSet(length, true);

    public uint Min { get; private set; } = uint.MaxValue;
    public uint Max { get; private set; } = uint.MinValue;
    public bool HasEven { get; private set; }
    public bool HasOdd { get; private set; }

    internal ulong[] Values => _tracker.BitSet;
    internal int BitCount { get; private set; }
    internal bool HasBitSet => _tracker.IsBitSet;

    internal bool Consecutive
    {
        get
        {
            if (BitCount == 0)
                return false;

            return BitCount == Max - Min + 1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool Get(uint index) => _tracker.Contains(index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool SetTrue(uint index)
    {
        bool added = _tracker.Add(index);

        if (added)
        {
            BitCount++;
            Min = Math.Min(Min, index);
            Max = Math.Max(Max, index);

            if ((index & 1) == 0)
                HasEven = true;
            else
                HasOdd = true;
        }

        return !added;
    }
}