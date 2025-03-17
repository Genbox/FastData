using System.Runtime.InteropServices;
using Genbox.FastData.Internal.Helpers;
using static System.Numerics.BitOperations;

namespace Genbox.FastData.Internal.Analysis.Misc;

[StructLayout(LayoutKind.Auto)]
internal struct IntegerBitSet
{
    internal ulong BitSet;

    internal readonly uint Count => (uint)PopCount(BitSet);
    internal readonly uint MinValue => (uint)(TrailingZeroCount(BitSet) + 1);
    internal readonly uint MaxValue => (uint)(64 - LeadingZeroCount(BitSet));
    internal readonly bool Consecutive => BitHelper.AreBitsConsecutive(BitSet);

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