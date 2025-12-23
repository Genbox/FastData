namespace Genbox.FastData.Internal.Misc;

/// <summary>This class starts out as a bitset, but when too many items are added, it switches to a HashSet</summary>
internal sealed class SwitchingBitSet
{
    private readonly int _maxBitSetWords;
    private readonly bool _offByOneMode; // "Off by one mode" is needed for length bitarrays as we occupy the first bit as length = 1
    private ulong[]? _bits;
    private HashSet<uint>? _set;

    internal SwitchingBitSet(int length, bool offByOneMode, int maxBitSetWords = 131072)
    {
        if (length <= 0)
            throw new InvalidOperationException("Length must be greater than zero.");

        _offByOneMode = offByOneMode;
        _maxBitSetWords = maxBitSetWords;

        int wordLength = GetWordLength(length);

        if (wordLength <= maxBitSetWords)
            _bits = new ulong[wordLength];
        else
            _set = new HashSet<uint>();
    }

    internal bool IsBitSet => _bits != null;
    internal ulong[] BitSet => _bits ?? [];

    internal bool Add(uint index)
    {
        if (_bits != null)
        {
            EnsureCapacityForIndex(index);

            if (_bits == null)
                return _set!.Add(index);

            GetPosition(index, out uint wordIndex, out ulong mask);

            if ((_bits[wordIndex] & mask) != 0)
                return false;

            _bits[wordIndex] |= mask;
            return true;
        }

        return _set!.Add(index);
    }

    internal bool Contains(uint index)
    {
        if (_bits != null)
        {
            GetPosition(index, out uint wordIndex, out ulong mask);

            if (wordIndex >= _bits.Length)
                return false;

            return (_bits[wordIndex] & mask) != 0;
        }

        return _set!.Contains(index);
    }

    private static int GetWordLength(int length) => (int)(((long)length + 63) >> 6);

    private void GetPosition(uint index, out uint wordIndex, out ulong mask)
    {
        wordIndex = index >> 6;

        if (_offByOneMode)
        {
            ulong remainder = index & 63;
            int bitIndex = remainder == 0 ? 63 : (int)remainder - 1;
            mask = 1UL << bitIndex;
        }
        else
        {
            mask = 1UL << ((int)index & 63);
        }
    }

    private void SwitchToSet()
    {
        HashSet<uint> set = new HashSet<uint>();

        for (int wordIndex = 0; wordIndex < _bits!.Length; wordIndex++)
        {
            ulong word = _bits[wordIndex];
            if (word == 0)
                continue;

            for (int bitIndex = 0; bitIndex < 64; bitIndex++)
            {
                if ((word & (1UL << bitIndex)) == 0)
                    continue;

                set.Add(GetIndex(wordIndex, bitIndex));
            }
        }

        _set = set;
        _bits = null;
    }

    private uint GetIndex(int wordIndex, int bitIndex)
    {
        if (_offByOneMode)
            return (uint)(((ulong)wordIndex << 6) + (bitIndex == 63 ? 0UL : (uint)bitIndex + 1));

        return (uint)(((ulong)wordIndex << 6) + (uint)bitIndex);
    }

    private void EnsureCapacityForIndex(uint index)
    {
        uint wordLength = (index >> 6) + 1;
        if (wordLength <= _bits!.Length)
            return;

        if (wordLength > _maxBitSetWords)
        {
            SwitchToSet();
            return;
        }

        Array.Resize(ref _bits, (int)wordLength);
    }
}