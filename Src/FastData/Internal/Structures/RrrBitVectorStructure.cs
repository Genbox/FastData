using System.Diagnostics;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Generators.EarlyExits.Exits;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Extensions;

namespace Genbox.FastData.Internal.Structures;

public sealed class RrrBitVectorStructure<TKey, TValue> : IStructure<TKey, TValue, RrrBitVectorContext>
{
    private const int BlockSize = 15;
    private readonly bool _keysAreSorted;
    private readonly TKey _maxValue;
    private readonly TKey _minValue;

    internal RrrBitVectorStructure(TKey minValue, TKey maxValue, bool keysAreSorted)
    {
        _minValue = minValue;
        _maxValue = maxValue;
        _keysAreSorted = keysAreSorted;
    }

    public RrrBitVectorContext? Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        Debug.Assert(!keys.IsEmpty, "RrrBitVectorStructure requires at least one key.");
        Debug.Assert(typeof(TKey) == typeof(char) || typeof(TKey) == typeof(byte) || typeof(TKey) == typeof(ushort) || typeof(TKey) == typeof(uint) || typeof(TKey) == typeof(ulong) || typeof(TKey) == typeof(sbyte) || typeof(TKey) == typeof(short) || typeof(TKey) == typeof(int) || typeof(TKey) == typeof(long), "RrrBitVectorStructure only supports integral key types.");
        Debug.Assert(!_keysAreSorted || keys.IsSorted(), "RrrBitVectorStructure requires sorted input when keysAreSorted is true.");

        ReadOnlySpan<TKey> span = keys.Span;
        ulong[] mapped = new ulong[span.Length];

        for (int i = 0; i < span.Length; i++)
            mapped[i] = MapKeyToSortable(span[i]);

        if (!_keysAreSorted)
            Array.Sort(mapped);

        ulong minValue = mapped[0];
        ulong maxValue = mapped[mapped.Length - 1];

        if (maxValue - minValue == ulong.MaxValue)
            return null; // We cannot produce the data structure. Try next.

        ulong universe = (maxValue - minValue) + 1UL;
        ulong blockCount64 = ((universe + BlockSize) - 1UL) / BlockSize;
        Debug.Assert(blockCount64 <= int.MaxValue, "RrrBitVectorStructure requires a block count that fits in an int-backed table.");

        int blockCount = (int)blockCount64;
        byte[] classes = new byte[blockCount];
        uint[] offsets = new uint[blockCount];

        int keyIndex = 0;

        for (int block = 0; block < blockCount; block++)
        {
            ushort mask = 0;

            while (keyIndex < mapped.Length)
            {
                ulong normalized = mapped[keyIndex] - minValue;
                ulong currentBlock = normalized / BlockSize;

                if (currentBlock != (ulong)block)
                    break;

                int bit = (int)(normalized % BlockSize);
                mask |= (ushort)(1 << bit);
                keyIndex++;
            }

            int classValue = PopCount(mask);
            classes[block] = (byte)classValue;

            if (classValue != 0)
                offsets[block] = RankMask(mask, classValue);
        }

        return new RrrBitVectorContext(minValue, maxValue, BlockSize, classes, offsets);
    }

    public IEnumerable<IEarlyExit> GetMandatoryExits()
    {
        yield return new ValueLessThanEarlyExit<TKey>(_minValue);
        yield return new ValueGreaterThanEarlyExit<TKey>(_maxValue);
    }

    private static uint RankMask(ushort mask, int classValue)
    {
        uint rank = 0;
        int remaining = classValue;

        for (int bit = BlockSize - 1; bit >= 0; bit--)
        {
            if (((mask >> bit) & 1) == 0)
                continue;

            rank += (uint)Binomial(bit, remaining);
            remaining--;

            if (remaining == 0)
                break;
        }

        return rank;
    }

    private static int PopCount(ushort value)
    {
        int count = 0;

        while (value != 0)
        {
            count += value & 1;
            value >>= 1;
        }

        return count;
    }

    private static int Binomial(int n, int k)
    {
        if (k < 0 || k > n)
            return 0;

        if (k == 0 || k == n)
            return 1;

        if (k > n - k)
            k = n - k;

        int result = 1;

        for (int i = 1; i <= k; i++)
            result = checked((result * (n - (k - i))) / i);

        return result;
    }

    private static ulong MapKeyToSortable(TKey key)
    {
        if (typeof(TKey) == typeof(char))
            return Cast<char>(key);

        if (typeof(TKey) == typeof(byte))
            return Cast<byte>(key);

        if (typeof(TKey) == typeof(ushort))
            return Cast<ushort>(key);

        if (typeof(TKey) == typeof(uint))
            return Cast<uint>(key);

        if (typeof(TKey) == typeof(ulong))
            return Cast<ulong>(key);

        if (typeof(TKey) == typeof(sbyte))
            return unchecked((byte)(Cast<sbyte>(key) ^ sbyte.MinValue));

        if (typeof(TKey) == typeof(short))
            return unchecked((ushort)(Cast<short>(key) ^ short.MinValue));

        if (typeof(TKey) == typeof(int))
            return unchecked((uint)(Cast<int>(key) ^ int.MinValue));

        if (typeof(TKey) == typeof(long))
            return unchecked((ulong)(Cast<long>(key) ^ long.MinValue));

        throw new InvalidOperationException("RRR bitvector only supports integral key types.");
    }

    private static T Cast<T>(TKey key) => (T)(object)key!;
}