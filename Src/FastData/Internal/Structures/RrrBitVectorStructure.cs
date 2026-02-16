using Genbox.FastData.Generators.Contexts;
using Genbox.FastData.Internal.Abstracts;

namespace Genbox.FastData.Internal.Structures;

internal sealed class RrrBitVectorStructure<TKey, TValue> : IStructure<TKey, TValue, RrrBitVectorContext>
{
    private const int BlockSize = 15;

    public RrrBitVectorContext Create(ReadOnlyMemory<TKey> keys, ReadOnlyMemory<TValue> values)
    {
        ReadOnlySpan<TKey> span = keys.Span;
        ulong[] mapped = new ulong[span.Length];

        for (int i = 0; i < span.Length; i++)
            mapped[i] = MapKeyToSortable(span[i]);

        Array.Sort(mapped);

        ulong minValue = mapped[0];
        ulong maxValue = mapped[mapped.Length - 1];
        ulong universe = maxValue - minValue + 1UL;
        ulong blockCount64 = (universe + (ulong)BlockSize - 1UL) / (ulong)BlockSize;

        if (blockCount64 > int.MaxValue)
            throw new InvalidOperationException("RRR bitvector is too large.");

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
                ulong currentBlock = normalized / (ulong)BlockSize;

                if (currentBlock != (ulong)block)
                    break;

                int bit = (int)(normalized % (ulong)BlockSize);
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
            result = checked(result * (n - (k - i)) / i);

        return result;
    }

    private static ulong MapKeyToSortable(TKey key)
    {
        if (typeof(TKey) == typeof(char))
            return (char)(object)key;

        if (typeof(TKey) == typeof(byte))
            return (byte)(object)key;

        if (typeof(TKey) == typeof(ushort))
            return (ushort)(object)key;

        if (typeof(TKey) == typeof(uint))
            return (uint)(object)key;

        if (typeof(TKey) == typeof(ulong))
            return (ulong)(object)key;

        if (typeof(TKey) == typeof(sbyte))
            return unchecked((byte)((sbyte)(object)key ^ sbyte.MinValue));

        if (typeof(TKey) == typeof(short))
            return unchecked((ushort)((short)(object)key ^ short.MinValue));

        if (typeof(TKey) == typeof(int))
            return unchecked((uint)((int)(object)key ^ int.MinValue));

        if (typeof(TKey) == typeof(long))
            return unchecked((ulong)((long)(object)key ^ long.MinValue));

        throw new InvalidOperationException("RRR bitvector only supports integral key types.");
    }
}
