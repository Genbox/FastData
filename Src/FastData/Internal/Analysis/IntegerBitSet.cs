using System.Runtime.InteropServices;
using Genbox.FastData.Internal.Compat;

namespace Genbox.FastData.Internal.Analysis;

[StructLayout(LayoutKind.Auto)]
internal struct IntegerBitSet
{
    private ulong _bitset;

    public uint Count => BitOperations.PopCount(_bitset);
    public uint MinValue => BitOperations.TrailingZeroCount(_bitset) + 1;
    public uint MaxValue => 64 - BitOperations.LeadingZeroCount(_bitset);

    public bool Contains(int val)
    {
        if (val >= 64)
            return false;

        return (_bitset & (1UL << (val - 1) % 64)) > 0;
    }

    public void Set(int val)
    {
        if (val >= 64)
            return;

        _bitset |= 1UL << ((val - 1) % 64);
    }
}