namespace Genbox.FastData.Benchmarks.Benchmarks;

public class RangeLookupBenchmarks
{
    private int[] _queries = null!;
    private int[] _rangeEnds = null!;
    private int[] _rangeStarts = null!;

    [Params(2, 4, 8, 16, 32, 64, 100)]
    public int RangeCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _rangeStarts = new int[RangeCount];
        _rangeEnds = new int[RangeCount];

        for (int i = 0; i < RangeCount; i++)
        {
            _rangeStarts[i] = i * 8;
            _rangeEnds[i] = _rangeStarts[i] + 3;
        }

        _queries = new int[1024];

        for (int i = 0; i < _queries.Length; i++)
            _queries[i] = (i * 37) % (RangeCount * 8);
    }

    [Benchmark(Baseline = true)]
    public int Linear()
    {
        int found = 0;

        foreach (int key in _queries)
        {
            for (int i = 0; i < _rangeStarts.Length; i++)
            {
                if (key < _rangeStarts[i])
                    break;

                if (key <= _rangeEnds[i])
                {
                    found++;
                    break;
                }
            }
        }

        return found;
    }

    [Benchmark]
    public int Binary()
    {
        int found = 0;

        foreach (int key in _queries)
        {
            int low = 0;
            int high = _rangeStarts.Length;

            while (low < high)
            {
                int middle = low + ((high - low) >> 1);

                if (key < _rangeStarts[middle])
                    high = middle;
                else
                    low = middle + 1;
            }

            int rangeIndex = low - 1;

            if (rangeIndex >= 0 && key <= _rangeEnds[rangeIndex])
                found++;
        }

        return found;
    }
}