namespace Genbox.FastData.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class DeduplicationBenchmarks
{
    private int[] _intKeys = null!;

    [GlobalSetup]
    public void Setup()
    {
        Random rng = new Random(42);

        _intKeys = new int[1000];
        for (int i = 0; i < _intKeys.Length; i++)
            _intKeys[i] = rng.Next(0, 200);
    }

    [Benchmark]
    public void HashSetDedup()
    {
        int[] keys = new int[_intKeys.Length];
        Array.Copy(_intKeys, keys, _intKeys.Length);
        FastDataGenerator.DeduplicateWithHashSet(keys, Array.Empty<int>(), false, EqualityComparer<int>.Default, out _);
    }

    [Benchmark]
    public void SortDedup()
    {
        int[] keys = new int[_intKeys.Length];
        Array.Copy(_intKeys, keys, _intKeys.Length);
        FastDataGenerator.DeduplicateWithSort(keys, Array.Empty<int>(), false, EqualityComparer<int>.Default, Comparer<int>.Default, out _);
    }

    [Benchmark]
    public void SortPreserveDedup()
    {
        int[] keys = new int[_intKeys.Length];
        Array.Copy(_intKeys, keys, _intKeys.Length);
        FastDataGenerator.DeduplicateWithSortPreserveInputOrder(keys, Array.Empty<int>(), false, EqualityComparer<int>.Default, Comparer<int>.Default, out _);
    }
}