using System.Runtime.CompilerServices;

namespace Genbox.FastData.Benchmarks.Benchmarks;

public class VectorBenchmarks
{
    [Benchmark]public bool Rrr() => RrrBitVectorStructure_Int32_1000.Contains(Random.Shared.Next(0, 1000));
    [Benchmark]public bool Ef() => EliasFanoStructure_Int32_1000.Contains(Random.Shared.Next(0, 1000));

    private static class RrrBitVectorStructure_Int32_1000
    {
        private const ulong _rrrMinValue = 2147483648ul;
        private const ulong _rrrMaxValue = 2147484647ul;
        private const int _rrrBlockSize = 15;
        private static readonly byte[] _rrrClasses = new byte[]
        {
            15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
            15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
            15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
            15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
            15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
            15, 15, 15, 15, 15, 15, 15, 15, 15, 15,
            15, 15, 15, 15, 15, 15, 10
        };
        private static readonly uint[] _rrrOffsets = new uint[]
        {
            uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue,
            uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue,
            uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue,
            uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue,
            uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue,
            uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue,
            uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue, uint.MinValue
        };

        public static bool Contains(int key)
        {
            if (((uint)key & 4294966272u) != 0)
                return false;

            ulong mapped = (ulong)(uint)(key ^ int.MinValue);

            if (mapped < _rrrMinValue || mapped > _rrrMaxValue)
                return false;

            ulong normalized = mapped - _rrrMinValue;
            int blockIndex = (int)(normalized / (ulong)_rrrBlockSize);
            int bitInBlock = (int)(normalized % (ulong)_rrrBlockSize);
            int classValue = _rrrClasses[blockIndex];

            if (classValue == 0)
                return false;

            uint rank = _rrrOffsets[blockIndex];
            return DecodeBit(classValue, rank, bitInBlock);
        }

        private static bool DecodeBit(int classValue, uint rank, int targetBit)
        {
            int remaining = classValue;

            for (int bit = _rrrBlockSize - 1; bit >= 0; bit--)
            {
                if (remaining == 0)
                    return false;

                int comb = Binomial(bit, remaining);
                bool isSet;

                if (rank >= (uint)comb)
                {
                    rank -= (uint)comb;
                    remaining--;
                    isSet = true;
                }
                else
                    isSet = false;

                if (bit == targetBit)
                    return isSet;
            }

            return false;
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

        public const uint ItemCount = 1000;
        public const int MinKey = 0;
        public const int MaxKey = 999;
    }

    private static class EliasFanoStructure_Int32_1000
    {
        private const int _lowerBitCount = 0;
        private static readonly ulong[] _upperBits = new ulong[]
        {
            6148914691236517205ul, 6148914691236517205ul, 6148914691236517205ul, 6148914691236517205ul, 6148914691236517205ul, 6148914691236517205ul, 6148914691236517205ul, 6148914691236517205ul, 6148914691236517205ul, 6148914691236517205ul,
            6148914691236517205ul, 6148914691236517205ul, 6148914691236517205ul, 6148914691236517205ul, 6148914691236517205ul, 6148914691236517205ul, 6148914691236517205ul, 6148914691236517205ul, 6148914691236517205ul, 6148914691236517205ul,
            6148914691236517205ul, 6148914691236517205ul, 6148914691236517205ul, 6148914691236517205ul, 6148914691236517205ul, 6148914691236517205ul, 6148914691236517205ul, 6148914691236517205ul, 6148914691236517205ul, 6148914691236517205ul,
            6148914691236517205ul, 21845ul
        };

        private const int _sampleRateShift = 7;
        private static readonly int[] _samplePositions = new int[]
        {
            0, 257, 513, 769, 1025, 1281, 1537, 1793
        };

        public static bool Contains(int key)
        {
            if (((uint)key & 4294966272u) != 0)
                return false;

            long value = (long)key;
            long high = value >> _lowerBitCount;

            long position = high == 0 ? 0 : SelectZero(high - 1) + 1;
            if (position < 0)
                return false;

            long rank = position - high;
            if ((ulong)rank >= ItemCount)
                return false;

            int currWord = (int)(position >> 6);

            if ((uint)currWord >= (uint)_upperBits.Length)
                return false;

            ulong window = _upperBits[currWord] & (ulong.MaxValue << (int)(position & 63));
            while (true)
            {
                while (window == 0)
                {
                    currWord++;
                    if ((uint)currWord >= (uint)_upperBits.Length)
                        return false;

                    window = _upperBits[currWord];
                }

                int trailing = System.Numerics.BitOperations.TrailingZeroCount(window);
                long onePosition = ((long)currWord << 6) + trailing;
                long currentHigh = onePosition - rank;

                if (currentHigh >= high)
                {
                    if (currentHigh > high)
                        return false;

                    return true;
                }

                window &= window - 1;
                rank++;

                if ((ulong)rank >= ItemCount)
                    return false;
            }
        }

        private static long SelectZero(long rank)
        {
            if (rank < 0)
                return -1;

            int sampleIndex = (int)(rank >> _sampleRateShift);
            if ((uint)sampleIndex >= (uint)_samplePositions.Length)
                return -1;

            long zeroRank = (long)sampleIndex << _sampleRateShift;
            int startPosition = _samplePositions[sampleIndex];
            int wordIndex = startPosition >> 6;
            int startBit = startPosition & 63;

            for (; wordIndex < _upperBits.Length; wordIndex++)
            {
                int validBits = wordIndex == _upperBits.Length - 1 ? 15 : 64;
                ulong validMask = validBits == 64 ? ulong.MaxValue : (1UL << validBits) - 1;
                ulong zeros = ~_upperBits[wordIndex] & validMask;

                if (startBit > 0)
                {
                    zeros &= ~((1UL << startBit) - 1);
                    startBit = 0;
                }

                int zeroCount = System.Numerics.BitOperations.PopCount(zeros);
                if (zeroCount == 0)
                    continue;

                if (zeroRank + zeroCount > rank)
                {
                    int rankInWord = (int)(rank - zeroRank);
                    int bitInWord = SelectBitInWord(zeros, rankInWord);
                    return ((long)wordIndex << 6) + bitInWord;
                }

                zeroRank += zeroCount;
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int SelectBitInWord(ulong word, int rank)
        {
            if ((uint)rank >= 64)
                return -1;

            int remaining = rank;
            ulong value = word;

            while (remaining > 0)
            {
                if (value == 0)
                    return -1;

                value &= value - 1;
                remaining--;
            }

            if (value == 0)
                return -1;

            return System.Numerics.BitOperations.TrailingZeroCount(value);
        }

        public const uint ItemCount = 1000;
        public const int MinKey = 0;
        public const int MaxKey = 999;
    }
}