using System.Runtime.CompilerServices;
using Genbox.FastData.Internal.Misc;

namespace Genbox.FastData.Internal.Analysis.Data;

internal sealed class LengthBitArray(int length = 64)
{
    private readonly SwitchingBitSet _tracker = new SwitchingBitSet(length, true);

    public int Min { get; private set; } = int.MaxValue;
    public int Max { get; private set; } = int.MinValue;

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
    internal bool Get(int index) => _tracker.Contains(index);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool SetTrue(int index)
    {
        if (index < 0)
            throw new ArgumentException("Index must be non-negative: " + index, nameof(index));

        unchecked
        {
            bool added = _tracker.Add(index);

            if (added)
            {
                BitCount++;
                Min = Math.Min(Min, index);
                Max = Math.Max(Max, index);
            }

            return !added;
        }
    }
}