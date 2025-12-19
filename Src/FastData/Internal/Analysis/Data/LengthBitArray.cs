using System.Runtime.CompilerServices;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Internal.Analysis.Data;

internal sealed class LengthBitArray(int length = 64)
{
    private int _length = length;
    private ulong[] _values = new ulong[GetLength(length)];

    internal ulong[] Values => _values;
    internal int BitCount { get; private set; }

    internal bool Consecutive
    {
        get
        {
            foreach (ulong val in _values)
            {
                if (!BitHelper.AreBitsConsecutive(val))
                    return false;
            }

            return true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal bool Get(int index)
    {
        if (unchecked((uint)index >= (uint)_length))
            throw new ArgumentException("Index out of range: " + index, nameof(index));

        return (_values[index >> 6] & (1UL << ((index & 63) - 1))) != 0; //-1 because we want a length of 1 to set the 0th bit
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
            ulong mask = 1UL << ((index & 63) - 1); //-1 because we want a length of 1 to set the 0th bit
            ref ulong slot = ref _values[index >> 6];
            bool alreadySet = (slot & mask) != 0;

            if (!alreadySet)
                BitCount++;

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
}