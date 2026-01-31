namespace Genbox.FastData.Benchmarks.Benchmarks;

public class TimeComplexity
{
    private int[] _data = null!;
    private int[] _eytzinger = null!;
    private HashSet<int> _hashSet = null!;

    [Params(1, 50, 100)]
    public int Query { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _data = Enumerable.Range(1, 100).ToArray();
        _eytzinger = BuildEytzinger(_data);
        _hashSet = new HashSet<int>(_data);
    }

    [Benchmark]public bool SwitchLookup() => SwitchSearch(Query);
    [Benchmark]public bool SwitchLookupAvg() => SwitchSearch(Random.Shared.Next(1, Query));

    [Benchmark]public bool LinearLookup() => _data.Contains(Query);
    [Benchmark]public bool LinearLookupAvg() => _data.Contains(Random.Shared.Next(1, Query));

    [Benchmark]public int BinarySearchLookup() => _data.BinarySearch(Query);
    [Benchmark]public int BinarySearchLookupAvg() => _data.BinarySearch(Random.Shared.Next(1, Query));

    [Benchmark]public int EytzingerLookup() => EytzingerSearch(_eytzinger, Query);
    [Benchmark]public int EytzingerLookupAvg() => EytzingerSearch(_eytzinger, Random.Shared.Next(1, Query));

    [Benchmark]public bool HashSetLookup() => _hashSet.Contains(Query);
    [Benchmark]public bool HashSetLookupAvg() => _hashSet.Contains(Random.Shared.Next(1, Query));

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