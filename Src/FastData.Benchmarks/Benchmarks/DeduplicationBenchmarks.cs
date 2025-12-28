namespace Genbox.FastData.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class DeduplicationBenchmarks
{
    private int[] _intKeys = [];
    private string[] _stringKeys = [];

    [Params(DeduplicationMode.Disabled, DeduplicationMode.HashSet, DeduplicationMode.Sort)]
    public DeduplicationMode Mode { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Random rng = new Random(42);

        _intKeys = new int[1000];
        for (int i = 0; i < _intKeys.Length; i++)
            _intKeys[i] = rng.Next(0, 200);

        _stringKeys = new string[1000];
        for (int i = 0; i < _stringKeys.Length; i++)
            _stringKeys[i] = "key" + rng.Next(0, 200);
    }

    [Benchmark]
    public void IntKeys()
    {
        FastDataConfig cfg = new FastDataConfig();
        cfg.DeduplicationMode = Mode;

        FastDataGenerator.DeduplicateKeys(cfg, _intKeys, ReadOnlyMemory<int>.Empty, EqualityComparer<int>.Default, Comparer<int>.Default, out _, out _, out int _);
    }

    [Benchmark]
    public void StringKeys()
    {
        FastDataConfig cfg = new FastDataConfig();
        cfg.DeduplicationMode = Mode;

        FastDataGenerator.DeduplicateKeys(cfg, _stringKeys, ReadOnlyMemory<int>.Empty, StringComparer.Ordinal, StringComparer.Ordinal, out _, out _, out int _);
    }
}