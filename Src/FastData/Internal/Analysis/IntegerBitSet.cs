using System.Runtime.InteropServices;
using Genbox.FastData.Internal.Compat;

namespace Genbox.FastData.Internal.Analysis;

[StructLayout(LayoutKind.Auto)]
internal struct IntegerBitSet
{
    internal ulong BitSet;

    internal readonly uint Count => BitOperations.PopCount(BitSet);
    internal readonly uint MinValue => BitOperations.TrailingZeroCount(BitSet) + 1;
    internal readonly uint MaxValue => 64 - BitOperations.LeadingZeroCount(BitSet);
    internal readonly bool Consecutive => BitOperations.AreBitsConsecutive(BitSet);

    internal readonly bool Contains(int val)
    {
        if (val >= 64)
            return false;

        return (BitSet & (1UL << (val - 1) % 64)) > 0;
    }

    internal void Set(int val)
    {
        if (val >= 64)
            return;

        BitSet |= 1UL << ((val - 1) % 64);
    }
}