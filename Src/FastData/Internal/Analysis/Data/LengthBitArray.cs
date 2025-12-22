using System.Runtime.CompilerServices;

namespace Genbox.FastData.Internal.Analysis.Data;

internal sealed class LengthBitArray(int length = 64)
{
    private int _length = length;
    private ulong[] _values = new ulong[GetLength(length)];

    public int Min { get; private set; } = int.MaxValue;
    public int Max { get; private set; } = int.MinValue;

    internal ulong[] Values => _values;
    internal int BitCount { get; private set; }

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
    internal bool Get(int index)
    {
        if (unchecked((uint)index >= (uint)_length))
            throw new ArgumentException("Index out of range: " + index, nameof(index));

        GetPosition(index, out int wordIndex, out ulong mask);
        return (_values[wordIndex] & mask) != 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool SetTrue(int index)
    {
        if (index < 0)
            throw new ArgumentException("Index must be non-negative: " + index, nameof(index));

        if (index >= _length)
            Expand(index + 1);

        unchecked
        {
            GetPosition(index, out int wordIndex, out ulong mask);
            ref ulong slot = ref _values[wordIndex];
            bool alreadySet = (slot & mask) != 0;

            if (!alreadySet)
            {
                BitCount++;
                Min = Math.Min(Min, index);
                Max = Math.Max(Max, index);
            }

            slot |= mask;
            return alreadySet;
        }
    }

    private void Expand(int newLength)
    {
        int newSize = GetLength(newLength);

        if (newSize > _values.Length)
            Array.Resize(ref _values, newSize);

        _length = newLength;
    }

    private static int GetLength(int n)
    {
        if (n == 0)
            throw new InvalidOperationException("Length must be greater than zero.");

        return (n + 63) >> 6;
    }

    private static void GetPosition(int index, out int wordIndex, out ulong mask)
    {
        int remainder = index & 63;
        int bitIndex = remainder == 0 ? 63 : remainder - 1; //-1 because we want a length of 1 to set the 0th bit

        wordIndex = index >> 6;
        mask = 1UL << bitIndex;
    }
}