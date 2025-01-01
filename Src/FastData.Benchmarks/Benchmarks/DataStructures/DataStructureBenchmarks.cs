using Genbox.FastFilter;

namespace Genbox.FastData.Benchmarks.Benchmarks.DataStructures;

public class DataStructureBenchmarks
{
    private const int _min = 1;
    private const int _mid = _max / 2;
    private const int _max = 100;

    private int[] _array = null!;
    private int[] _eytzinger = null!;

    private HashSet<int> _hashSet = null!;
    private BloomFilter<int> _bloom = null!;
    private BlockedBloomFilter<int> _blockedBloom = null!;
    private BinaryFuse8Filter<int> _binaryFuse8 = null!;

    [GlobalSetup(Targets = [nameof(ArrayMin), nameof(ArrayMid), nameof(ArrayMax), nameof(BinarySearchMin), nameof(BinarySearchMid), nameof(BinarySearchMax)])]
    public void SetupArray() => _array = Enumerable.Range(_min, _max).ToArray();

    [GlobalSetup(Targets = [nameof(EytzingerSearchMin), nameof(EytzingerSearchMid), nameof(EytzingerSearchMax)])]
    public void SetupEytzinger()
    {
        _eytzinger = new int[_max];

        int idx = 0;
        EytzingerOrder(Enumerable.Range(_min, _max).ToArray(), _eytzinger, ref idx);
    }

    [GlobalSetup(Targets = [nameof(HashSetMin), nameof(HashSetMid), nameof(HashSetMax)])]
    public void SetupHashSet() => _hashSet = new HashSet<int>(Enumerable.Range(_min, _max));

    [GlobalSetup(Targets = [nameof(BloomMin), nameof(BloomMid), nameof(BloomMax)])]
    public void SetupBloomFilter() => _bloom = new BloomFilter<int>(Enumerable.Range(_min, _max).ToArray());

    [GlobalSetup(Targets = [nameof(BlockBloomMin), nameof(BlockBloomMid), nameof(BlockBloomMax)])]
    public void SetupBlockedBloomFilter() => _blockedBloom = new BlockedBloomFilter<int>(Enumerable.Range(_min, _max).ToArray());

    [GlobalSetup(Targets = [nameof(BinaryFuse8Min), nameof(BinaryFuse8Mid), nameof(BinaryFuse8Max)])]
    public void SetupBinaryFuse8Filter() => _binaryFuse8 = new BinaryFuse8Filter<int>(Enumerable.Range(_min, _max).ToArray());

    [Benchmark]public bool ArrayMin() => _array.Contains(_min);
    [Benchmark]public bool ArrayMid() => _array.Contains(_mid);
    [Benchmark]public bool ArrayMax() => _array.Contains(_max);

    [Benchmark]public int BinarySearchMin() => Array.BinarySearch(_array, _min);
    [Benchmark]public int BinarySearchMid() => Array.BinarySearch(_array, _mid);
    [Benchmark]public int BinarySearchMax() => Array.BinarySearch(_array, _max);

    [Benchmark]public bool EytzingerSearchMin() => EytzingerSearch(_eytzinger, _min);
    [Benchmark]public bool EytzingerSearchMid() => EytzingerSearch(_eytzinger, _mid);
    [Benchmark]public bool EytzingerSearchMax() => EytzingerSearch(_eytzinger, _max);

    [Benchmark]public bool HashSetMin() => _hashSet.Contains(_min);
    [Benchmark]public bool HashSetMid() => _hashSet.Contains(_mid);
    [Benchmark]public bool HashSetMax() => _hashSet.Contains(_max);

    [Benchmark]public bool IfElseMin() => IfElse(_min);
    [Benchmark]public bool IfElseMid() => IfElse(_mid);
    [Benchmark]public bool IfElseMax() => IfElse(_max);

    [Benchmark]public bool SwitchMin() => Switch(_min);
    [Benchmark]public bool SwitchMid() => Switch(_mid);
    [Benchmark]public bool SwitchMax() => Switch(_max);

    [Benchmark]public bool BloomMin() => _bloom.Contains(_min);
    [Benchmark]public bool BloomMid() => _bloom.Contains(_mid);
    [Benchmark]public bool BloomMax() => _bloom.Contains(_max);

    [Benchmark]public bool BlockBloomMin() => _blockedBloom.Contains(_min);
    [Benchmark]public bool BlockBloomMid() => _blockedBloom.Contains(_mid);
    [Benchmark]public bool BlockBloomMax() => _blockedBloom.Contains(_max);

    [Benchmark]public bool BinaryFuse8Min() => _binaryFuse8.Contains(_min);
    [Benchmark]public bool BinaryFuse8Mid() => _binaryFuse8.Contains(_mid);
    [Benchmark]public bool BinaryFuse8Max() => _binaryFuse8.Contains(_max);

    private static bool Switch(int value)
    {
        return value switch
        {
            1 or 2 or 3 or 4 or 5 or 6 or 7 or 8 or 9 or 10 or 11 or 12 or 13 or 14 or 15 or 16 or 17 or 18 or 19 or 20 or 21 or 22 or 23 or 24 or 25 or 26 or 27 or 28 or 29 or 30 or 31 or 32 or 33 or 34 or 35 or 36 or 37 or 38 or 39 or 40 or 41 or 42 or 43 or 44 or 45 or 46 or 47 or 48 or 49 or 50 or 51 or 52 or 53 or 54 or 55 or 56 or 57 or 58 or 59 or 60 or 61 or 62 or 63 or 64 or 65 or 66 or 67 or 68 or 69 or 70 or 71 or 72 or 73 or 74 or 75 or 76 or 77 or 78 or 79 or 80 or 81 or 82 or 83 or 84 or 85 or 86 or 87 or 88 or 89 or 90 or 91 or 92 or 93 or 94 or 95 or 96 or 97 or 98 or 99 or 100 => true,
            _ => false
        };
    }

    private static bool IfElse(int value)
    {
        if (value == 1) return true;
        if (value == 2) return true;
        if (value == 3) return true;
        if (value == 4) return true;
        if (value == 5) return true;
        if (value == 6) return true;
        if (value == 7) return true;
        if (value == 8) return true;
        if (value == 9) return true;
        if (value == 10) return true;
        if (value == 11) return true;
        if (value == 12) return true;
        if (value == 13) return true;
        if (value == 14) return true;
        if (value == 15) return true;
        if (value == 16) return true;
        if (value == 17) return true;
        if (value == 18) return true;
        if (value == 19) return true;
        if (value == 20) return true;
        if (value == 21) return true;
        if (value == 22) return true;
        if (value == 23) return true;
        if (value == 24) return true;
        if (value == 25) return true;
        if (value == 26) return true;
        if (value == 27) return true;
        if (value == 28) return true;
        if (value == 29) return true;
        if (value == 30) return true;
        if (value == 31) return true;
        if (value == 32) return true;
        if (value == 33) return true;
        if (value == 34) return true;
        if (value == 35) return true;
        if (value == 36) return true;
        if (value == 37) return true;
        if (value == 38) return true;
        if (value == 39) return true;
        if (value == 40) return true;
        if (value == 41) return true;
        if (value == 42) return true;
        if (value == 43) return true;
        if (value == 44) return true;
        if (value == 45) return true;
        if (value == 46) return true;
        if (value == 47) return true;
        if (value == 48) return true;
        if (value == 49) return true;
        if (value == 50) return true;
        if (value == 51) return true;
        if (value == 52) return true;
        if (value == 53) return true;
        if (value == 54) return true;
        if (value == 55) return true;
        if (value == 56) return true;
        if (value == 57) return true;
        if (value == 58) return true;
        if (value == 59) return true;
        if (value == 60) return true;
        if (value == 61) return true;
        if (value == 62) return true;
        if (value == 63) return true;
        if (value == 64) return true;
        if (value == 65) return true;
        if (value == 66) return true;
        if (value == 67) return true;
        if (value == 68) return true;
        if (value == 69) return true;
        if (value == 70) return true;
        if (value == 71) return true;
        if (value == 72) return true;
        if (value == 73) return true;
        if (value == 74) return true;
        if (value == 75) return true;
        if (value == 76) return true;
        if (value == 77) return true;
        if (value == 78) return true;
        if (value == 79) return true;
        if (value == 80) return true;
        if (value == 81) return true;
        if (value == 82) return true;
        if (value == 83) return true;
        if (value == 84) return true;
        if (value == 85) return true;
        if (value == 86) return true;
        if (value == 87) return true;
        if (value == 88) return true;
        if (value == 89) return true;
        if (value == 90) return true;
        if (value == 91) return true;
        if (value == 92) return true;
        if (value == 93) return true;
        if (value == 94) return true;
        if (value == 95) return true;
        if (value == 96) return true;
        if (value == 97) return true;
        if (value == 98) return true;
        if (value == 99) return true;
        if (value == 100) return true;
        return false;
    }

    private static void EytzingerOrder<T>(T[] input, T[] output, ref int arrIdx, int eytIdx = 0)
    {
        if (eytIdx < input.Length)
        {
            EytzingerOrder(input, output, ref arrIdx, (2 * eytIdx) + 1);
            output[eytIdx] = input[arrIdx++];
            EytzingerOrder(input, output, ref arrIdx, (2 * eytIdx) + 2);
        }
    }

    private static bool EytzingerSearch(int[] arr, int value)
    {
        int i = 0;
        while (i < (uint)arr.Length)
        {
            int comparison = arr[i] - value;

            if (comparison == 0)
                return true;

            if (comparison < 0)
                i = 2 * i + 2;
            else
                i = 2 * i + 1;
        }

        return false;
    }
}