using System.Runtime.CompilerServices;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Internal.Analysis.Data;

internal sealed class LengthBitArray(int length = 64)
{
    private ulong[] _array = new ulong[GetLength(length)];
    private int _length = length;

    public ulong FirstValue => _array[0];
    public int Count { get; private set; }

    public bool Consecutive
    {
        get
        {
            foreach (ulong val in _array)
            {
                if (!BitHelper.AreBitsConsecutive(val))
                    return false;
            }

            return true;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Get(int index)
    {
        if (unchecked((uint)index >= (uint)_length))
            throw new ArgumentException("Index out of range: " + index, nameof(index));

        return (_array[index >> 6] & (1UL << ((index & 63) - 1))) != 0; //-1 because we want a length of 1 to set the 0th bit
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool SetTrue(int index)
    {
        if (index < 0)
            throw new ArgumentException("Index must be non-negative: " + index, nameof(index));

        if (index >= _length)
            Expand(index + 1);

        unchecked
        {
            ulong mask = 1UL << ((index & 63) - 1); //-1 because we want a length of 1 to set the 0th bit
            ref ulong slot = ref _array[index >> 6];
            bool alreadySet = (slot & mask) != 0;

            if (!alreadySet)
                Count++;

            slot |= mask;
            return alreadySet;
        }
    }

    private void Expand(int newLength)
    {
        int newSize = GetLength(newLength);

        if (newSize > _array.Length)
            Array.Resize(ref _array, newSize);

        _length = newLength;
    }

    private static int GetLength(int n)
    {
        if (n == 0)
            throw new InvalidOperationException("Length must be greater than zero.");

        return (n + 63) >> 6;
    }
}