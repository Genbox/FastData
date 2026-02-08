using System.Numerics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Genbox.FastData.Benchmarks.Benchmarks;

public class TimeComplexityAvg
{
    private int[] _data = null!;
    private int[] _eytzinger = null!;
    private HashSet<int> _hashSet = null!;
    private int[] _k16Keys = null!;
    private int[] _k16Children = null!;
    private byte[] _k16KeyCounts = null!;

    [GlobalSetup]
    public void Setup()
    {
        _data = Enumerable.Range(1, 10000).ToArray();
        _eytzinger = BuildEytzinger(_data);
        _hashSet = new HashSet<int>(_data);
        BuildK16Tree(_data, out _k16Keys, out _k16Children, out _k16KeyCounts);
    }

    [Benchmark]public void SwitchLookup()
    {
        for (int i = 0; i < 10000; i++)
            SwitchSearch(Random.Shared.Next(1, _data.Length));
    }

    [Benchmark]public void LinearLookup()
    {
        for (int i = 0; i < 10000; i++)
            _data.Contains(Random.Shared.Next(1, _data.Length));
    }

    [Benchmark]public void BinarySearchLookup()
    {
        for (int i = 0; i < 10000; i++)
            _data.BinarySearch(Random.Shared.Next(1, _data.Length));
    }

    [Benchmark]public void InterpolationSearchLookup()
    {
        for (int i = 0; i < 10000; i++)
            InterpolationSearch(_data, Random.Shared.Next(1, _data.Length));
    }

    [Benchmark]public void EytzingerLookup()
    {
        for (int i = 0; i < 10000; i++)
            EytzingerSearch(_eytzinger, Random.Shared.Next(1, _data.Length));
    }

    [Benchmark]public void HashSetLookup()
    {
        for (int i = 0; i < 10000; i++)
            _hashSet.Contains(Random.Shared.Next(1, _data.Length));
    }

    [Benchmark]public void K16LookupLinearCount()
    {
        for (int i = 0; i < 10000; i++)
            K16SearchLinearCount(_k16Keys, _k16Children, _k16KeyCounts, Random.Shared.Next(1, _data.Length));
    }

    [Benchmark]public void K16LookupBranchlessUnrolled()
    {
        for (int i = 0; i < 10000; i++)
            K16SearchBranchlessUnrolled(_k16Keys, _k16Children, Random.Shared.Next(1, _data.Length));
    }

    [Benchmark]public void K16LookupBranchLean()
    {
        for (int i = 0; i < 10000; i++)
            K16SearchBranchLean(_k16Keys, _k16Children, Random.Shared.Next(1, _data.Length));
    }

    [Benchmark]public void K16LookupBinaryNode()
    {
        for (int i = 0; i < 10000; i++)
            K16SearchBinaryNode(_k16Keys, _k16Children, Random.Shared.Next(1, _data.Length));
    }

    [Benchmark]public void K16LookupSimd128()
    {
        for (int i = 0; i < 10000; i++)
            K16SearchSimd128(_k16Keys, _k16Children, Random.Shared.Next(1, _data.Length));
    }

    [Benchmark]public void K16LookupSimd256()
    {
        for (int i = 0; i < 10000; i++)
            K16SearchSimd256(_k16Keys, _k16Children, Random.Shared.Next(1, _data.Length));
    }

    [Benchmark]public void CompressedBloom()
    {
        for (int i = 0; i < 10000; i++)
            CompressedBloomFilterStructure_Int32_100.Contains(Random.Shared.Next(1, _data.Length));
    }

    [Benchmark]public void EliasFanoEncoded()
    {
        for (int i = 0; i < 10000; i++)
            EliasFanoSetStructure_Int32_100.Contains(Random.Shared.Next(1, _data.Length));
    }

    private static class EliasFanoSetStructure_Int32_100
    {
        private const ulong _efMinValue = 2147483648ul;
        private const ulong _efMaxValue = 2147483747ul;
        private const int _efLowerBitCount = 0;
        private const int _efItemCount = 100;
        private static readonly ulong[] _efHighBits =
        [
            6148914691236517205ul, 6148914691236517205ul, 6148914691236517205ul, 85ul
        ];

        public static bool Contains(int key)
        {
            ulong mapped = (uint)(key ^ int.MinValue);

            if (mapped is < _efMinValue or > _efMaxValue)
                return false;

            ulong target = mapped - _efMinValue;
            int low = 0;
            int high = _efItemCount - 1;

            while (low <= high)
            {
                int mid = low + ((high - low) >> 1);
                ulong value = GetValueAt(mid);

                if (value == target)
                    return true;

                if (value < target)
                    low = mid + 1;
                else
                    high = mid - 1;
            }

            return false;
        }

        private static ulong GetValueAt(int index)
        {
            int selectedBit = Select1(index);
            ulong high = (ulong)(selectedBit - index);
            return (high << _efLowerBitCount) | 0UL;
        }

        private static int Select1(int index)
        {
            int remaining = index;

            for (int i = 0; i < _efHighBits.Length; i++)
            {
                ulong word = _efHighBits[i];
                int count = CountBits(word);

                if (remaining < count)
                    return (i << 6) + SelectInWord(word, remaining);

                remaining -= count;
            }

            throw new InvalidOperationException("Elias-Fano select out of range.");
        }

        private static int SelectInWord(ulong word, int rank)
        {
            for (int bit = 0; bit < 64; bit++)
            {
                if (((word >> bit) & 1UL) == 0)
                    continue;

                if (rank == 0)
                    return bit;

                rank--;
            }

            throw new InvalidOperationException("Elias-Fano word rank out of range.");
        }

        private static int CountBits(ulong value)
        {
            value -= (value >> 1) & 0x5555555555555555UL;
            value = (value & 0x3333333333333333UL) + ((value >> 2) & 0x3333333333333333UL);
            value = (value + (value >> 4)) & 0x0F0F0F0F0F0F0F0FUL;
            return (int)((value * 0x0101010101010101UL) >> 56);
        }
    }

    private static class CompressedBloomFilterStructure_Int32_100
    {
        private static readonly int[] _compressedIndices =
        [
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9,
            10, 11, 12, 13, 14, 15
        ];

        private static readonly ulong[] _compressedWords =
        [
            281479271743489ul, 562958543486979ul, 1125917086973957ul, 2251834173947913ul, 4503668347895825ul, 9007336695791649ul, 18014673391583297ul, 36029346783166593ul, 72058693566333185ul, 144117387132666369ul,
            288234774265332737ul, 576469548530665473ul, 1152939097061330945ul, 2305878194122661889ul, 4611756388245323777ul, 9223512776490647553ul
        ];

        public static bool Contains(int key)
        {
            ulong hash = (ulong)key;
            uint index = (uint)(hash & 15);
            int target = (int)index;
            int low = 0;
            int high = _compressedIndices.Length - 1;
            ulong word = 0;

            while (low <= high)
            {
                int mid = low + ((high - low) >> 1);
                int value = _compressedIndices[mid];

                if (value == target)
                {
                    word = _compressedWords[mid];
                    break;
                }

                if (value < target)
                    low = mid + 1;
                else
                    high = mid - 1;
            }

            uint shift1 = (uint)(hash & 63UL);
            uint shift2 = (uint)((hash >> 8) & 63UL);
            ulong mask = (1UL << (int)shift1) | (1UL << (int)shift2);
            return (word & mask) == mask;
        }
    }

    private static int[] BuildEytzinger(int[] sorted)
    {
        int[] eytzinger = new int[sorted.Length];
        int index = 0;
        BuildEytzinger(sorted, eytzinger, 0, ref index);
        return eytzinger;
    }

    private static void BuildEytzinger(int[] sorted, int[] eytzinger, int node, ref int index)
    {
        if (node >= eytzinger.Length)
            return;

        BuildEytzinger(sorted, eytzinger, node * 2 + 1, ref index);
        eytzinger[node] = sorted[index];
        index++;
        BuildEytzinger(sorted, eytzinger, node * 2 + 2, ref index);
    }

    private static int EytzingerSearch(int[] eytzinger, int value)
    {
        int node = 0;
        int length = eytzinger.Length;

        while (node < length)
        {
            int current = eytzinger[node];

            if (value == current)
                return node;

            node = value < current ? node * 2 + 1 : node * 2 + 2;
        }

        return -1;
    }

    private static int InterpolationSearch(int[] sorted, int value)
    {
        int low = 0;
        int high = sorted.Length - 1;

        while (low <= high && value >= sorted[low] && value <= sorted[high])
        {
            int lowValue = sorted[low];
            int highValue = sorted[high];

            if (lowValue == highValue)
                return value == lowValue ? low : -1;

            int pos = low + (int)((long)(value - lowValue) * (high - low) / (highValue - lowValue));
            int current = sorted[pos];

            if (current == value)
                return pos;

            if (current < value)
                low = pos + 1;
            else
                high = pos - 1;
        }

        return -1;
    }

    private static void BuildK16Tree(int[] sorted, out int[] keys, out int[] children, out byte[] keyCounts)
    {
        int nodeCount = CountK16Nodes(sorted.Length);
        keys = new int[nodeCount * 16];
        children = new int[nodeCount * 17];
        Array.Fill(keys, int.MaxValue);
        Array.Fill(children, -1);
        keyCounts = new byte[nodeCount];

        int nextNode = 0;
        BuildK16Node(sorted, 0, sorted.Length, ref nextNode, keys, children, keyCounts);
    }

    private static int CountK16Nodes(int length)
    {
        if (length <= 0)
            return 0;

        if (length <= 16)
            return 1;

        int remaining = length - 16;
        int baseChild = remaining / 17;
        int remainder = remaining % 17;
        int count = 1;

        for (int i = 0; i < 17; i++)
        {
            int childSize = baseChild + (i < remainder ? 1 : 0);
            count += CountK16Nodes(childSize);
        }

        return count;
    }

    private static int BuildK16Node(int[] sorted, int start, int length, ref int nextNode, int[] keys, int[] children, byte[] keyCounts)
    {
        if (length == 0)
            return -1;

        int nodeIndex = nextNode;
        nextNode++;

        int keyBase = nodeIndex * 16;
        int childBase = nodeIndex * 17;

        if (length <= 16)
        {
            for (int i = 0; i < length; i++)
                keys[keyBase + i] = sorted[start + i];

            keyCounts[nodeIndex] = (byte)length;

            return nodeIndex;
        }

        keyCounts[nodeIndex] = 16;

        int remaining = length - 16;
        int baseChild = remaining / 17;
        int remainder = remaining % 17;
        int cursor = start;

        for (int keyIndex = 0; keyIndex < 16; keyIndex++)
        {
            int childSize = baseChild + (keyIndex < remainder ? 1 : 0);
            int childIndex = BuildK16Node(sorted, cursor, childSize, ref nextNode, keys, children, keyCounts);

            if (childIndex != -1)
                children[childBase + keyIndex] = childIndex;

            cursor += childSize;
            keys[keyBase + keyIndex] = sorted[cursor];
            cursor++;
        }

        int lastChildSize = baseChild + (16 < remainder ? 1 : 0);
        int lastChildIndex = BuildK16Node(sorted, cursor, lastChildSize, ref nextNode, keys, children, keyCounts);

        if (lastChildIndex != -1)
            children[childBase + 16] = lastChildIndex;

        return nodeIndex;
    }

    private static int K16SearchLinearCount(int[] keys, int[] children, byte[] keyCounts, int value)
    {
        int node = 0;

        while (node != -1)
        {
            int keyBase = node * 16;
            int childBase = node * 17;
            int childSlot = 0;
            int count = keyCounts[node];

            for (int i = 0; i < count; i++)
            {
                int key = keys[keyBase + i];

                if (value == key)
                    return node;

                if (value > key)
                    childSlot++;
                else
                    break;
            }

            node = children[childBase + childSlot];
        }

        return -1;
    }

    private static int K16SearchBranchlessUnrolled(int[] keys, int[] children, int value)
    {
        int node = 0;

        while (node != -1)
        {
            int keyBase = node * 16;
            int childBase = node * 17;
            int k0 = keys[keyBase];
            int k1 = keys[keyBase + 1];
            int k2 = keys[keyBase + 2];
            int k3 = keys[keyBase + 3];
            int k4 = keys[keyBase + 4];
            int k5 = keys[keyBase + 5];
            int k6 = keys[keyBase + 6];
            int k7 = keys[keyBase + 7];
            int k8 = keys[keyBase + 8];
            int k9 = keys[keyBase + 9];
            int k10 = keys[keyBase + 10];
            int k11 = keys[keyBase + 11];
            int k12 = keys[keyBase + 12];
            int k13 = keys[keyBase + 13];
            int k14 = keys[keyBase + 14];
            int k15 = keys[keyBase + 15];

            int eq = (value == k0 ? 1 : 0)
                     | (value == k1 ? 1 : 0)
                     | (value == k2 ? 1 : 0)
                     | (value == k3 ? 1 : 0)
                     | (value == k4 ? 1 : 0)
                     | (value == k5 ? 1 : 0)
                     | (value == k6 ? 1 : 0)
                     | (value == k7 ? 1 : 0)
                     | (value == k8 ? 1 : 0)
                     | (value == k9 ? 1 : 0)
                     | (value == k10 ? 1 : 0)
                     | (value == k11 ? 1 : 0)
                     | (value == k12 ? 1 : 0)
                     | (value == k13 ? 1 : 0)
                     | (value == k14 ? 1 : 0)
                     | (value == k15 ? 1 : 0);

            if (eq != 0)
                return node;

            int childSlot = (value > k0 ? 1 : 0)
                            + (value > k1 ? 1 : 0)
                            + (value > k2 ? 1 : 0)
                            + (value > k3 ? 1 : 0)
                            + (value > k4 ? 1 : 0)
                            + (value > k5 ? 1 : 0)
                            + (value > k6 ? 1 : 0)
                            + (value > k7 ? 1 : 0)
                            + (value > k8 ? 1 : 0)
                            + (value > k9 ? 1 : 0)
                            + (value > k10 ? 1 : 0)
                            + (value > k11 ? 1 : 0)
                            + (value > k12 ? 1 : 0)
                            + (value > k13 ? 1 : 0)
                            + (value > k14 ? 1 : 0)
                            + (value > k15 ? 1 : 0);

            node = children[childBase + childSlot];
        }

        return -1;
    }

    private static int K16SearchBranchLean(int[] keys, int[] children, int value)
    {
        int node = 0;

        while (node != -1)
        {
            int keyBase = node * 16;
            int childBase = node * 17;
            int childSlot = 0;

            for (int i = 0; i < 16; i++)
            {
                int key = keys[keyBase + i];

                if (value == key)
                    return node;

                if (value > key)
                    childSlot++;
                else
                    break;
            }

            node = children[childBase + childSlot];
        }

        return -1;
    }

    private static int K16SearchBinaryNode(int[] keys, int[] children, int value)
    {
        int node = 0;

        while (node != -1)
        {
            int keyBase = node * 16;
            int childBase = node * 17;
            int lo = 0;
            int hi = 16;

            while (lo < hi)
            {
                int mid = lo + ((hi - lo) / 2);
                int key = keys[keyBase + mid];

                if (value > key)
                    lo = mid + 1;
                else
                    hi = mid;
            }

            if (lo < 16 && keys[keyBase + lo] == value)
                return node;

            node = children[childBase + lo];
        }

        return -1;
    }

    private static int K16SearchSimd128(int[] keys, int[] children, int value)
    {
        if (!Sse2.IsSupported)
            return K16SearchBranchlessUnrolled(keys, children, value);

        int node = 0;
        Vector128<int> valueVector = Vector128.Create(value);

        while (node != -1)
        {
            int keyBase = node * 16;
            int childBase = node * 17;

            Vector128<int> v0 = Vector128.Create(keys[keyBase], keys[keyBase + 1], keys[keyBase + 2], keys[keyBase + 3]);
            Vector128<int> v1 = Vector128.Create(keys[keyBase + 4], keys[keyBase + 5], keys[keyBase + 6], keys[keyBase + 7]);
            Vector128<int> v2 = Vector128.Create(keys[keyBase + 8], keys[keyBase + 9], keys[keyBase + 10], keys[keyBase + 11]);
            Vector128<int> v3 = Vector128.Create(keys[keyBase + 12], keys[keyBase + 13], keys[keyBase + 14], keys[keyBase + 15]);

            Vector128<int> eq0 = Sse2.CompareEqual(valueVector, v0);
            Vector128<int> eq1 = Sse2.CompareEqual(valueVector, v1);
            Vector128<int> eq2 = Sse2.CompareEqual(valueVector, v2);
            Vector128<int> eq3 = Sse2.CompareEqual(valueVector, v3);

            ulong eqMask = (ulong)Sse2.MoveMask(eq0.AsByte())
                           | ((ulong)Sse2.MoveMask(eq1.AsByte()) << 16)
                           | ((ulong)Sse2.MoveMask(eq2.AsByte()) << 32)
                           | ((ulong)Sse2.MoveMask(eq3.AsByte()) << 48);

            if (eqMask != 0)
                return node;

            Vector128<int> gt0 = Sse2.CompareGreaterThan(valueVector, v0);
            Vector128<int> gt1 = Sse2.CompareGreaterThan(valueVector, v1);
            Vector128<int> gt2 = Sse2.CompareGreaterThan(valueVector, v2);
            Vector128<int> gt3 = Sse2.CompareGreaterThan(valueVector, v3);

            uint mask0 = (uint)Sse2.MoveMask(gt0.AsByte());
            uint mask1 = (uint)Sse2.MoveMask(gt1.AsByte());
            uint mask2 = (uint)Sse2.MoveMask(gt2.AsByte());
            uint mask3 = (uint)Sse2.MoveMask(gt3.AsByte());

            int childSlot = (BitOperations.PopCount(mask0)
                             + BitOperations.PopCount(mask1)
                             + BitOperations.PopCount(mask2)
                             + BitOperations.PopCount(mask3)) / 4;

            node = children[childBase + childSlot];
        }

        return -1;
    }

    private static int K16SearchSimd256(int[] keys, int[] children, int value)
    {
        if (!Avx2.IsSupported)
            return K16SearchSimd128(keys, children, value);

        int node = 0;
        Vector256<int> valueVector = Vector256.Create(value);

        while (node != -1)
        {
            int keyBase = node * 16;
            int childBase = node * 17;

            Vector256<int> v0 = Vector256.Create(keys[keyBase], keys[keyBase + 1], keys[keyBase + 2], keys[keyBase + 3], keys[keyBase + 4], keys[keyBase + 5], keys[keyBase + 6], keys[keyBase + 7]);
            Vector256<int> v1 = Vector256.Create(keys[keyBase + 8], keys[keyBase + 9], keys[keyBase + 10], keys[keyBase + 11], keys[keyBase + 12], keys[keyBase + 13], keys[keyBase + 14], keys[keyBase + 15]);

            Vector256<int> eq0 = Avx2.CompareEqual(valueVector, v0);
            Vector256<int> eq1 = Avx2.CompareEqual(valueVector, v1);
            ulong eqMask = (ulong)Avx2.MoveMask(eq0.AsByte())
                           | ((ulong)Avx2.MoveMask(eq1.AsByte()) << 32);

            if (eqMask != 0)
                return node;

            Vector256<int> gt0 = Avx2.CompareGreaterThan(valueVector, v0);
            Vector256<int> gt1 = Avx2.CompareGreaterThan(valueVector, v1);
            uint mask0 = (uint)Avx2.MoveMask(gt0.AsByte());
            uint mask1 = (uint)Avx2.MoveMask(gt1.AsByte());

            int childSlot = (BitOperations.PopCount(mask0)
                             + BitOperations.PopCount(mask1)) / 4;

            node = children[childBase + childSlot];
        }

        return -1;
    }

    private static bool SwitchSearch(int key)
    {
        switch (key)
        {
            case 0:
            case 1:
            case 2:
            case 3:
            case 4:
            case 5:
            case 6:
            case 7:
            case 8:
            case 9:
            case 10:
            case 11:
            case 12:
            case 13:
            case 14:
            case 15:
            case 16:
            case 17:
            case 18:
            case 19:
            case 20:
            case 21:
            case 22:
            case 23:
            case 24:
            case 25:
            case 26:
            case 27:
            case 28:
            case 29:
            case 30:
            case 31:
            case 32:
            case 33:
            case 34:
            case 35:
            case 36:
            case 37:
            case 38:
            case 39:
            case 40:
            case 41:
            case 42:
            case 43:
            case 44:
            case 45:
            case 46:
            case 47:
            case 48:
            case 49:
            case 50:
            case 51:
            case 52:
            case 53:
            case 54:
            case 55:
            case 56:
            case 57:
            case 58:
            case 59:
            case 60:
            case 61:
            case 62:
            case 63:
            case 64:
            case 65:
            case 66:
            case 67:
            case 68:
            case 69:
            case 70:
            case 71:
            case 72:
            case 73:
            case 74:
            case 75:
            case 76:
            case 77:
            case 78:
            case 79:
            case 80:
            case 81:
            case 82:
            case 83:
            case 84:
            case 85:
            case 86:
            case 87:
            case 88:
            case 89:
            case 90:
            case 91:
            case 92:
            case 93:
            case 94:
            case 95:
            case 96:
            case 97:
            case 98:
            case 99:
                return true;
            default:
                return false;
        }
    }
}