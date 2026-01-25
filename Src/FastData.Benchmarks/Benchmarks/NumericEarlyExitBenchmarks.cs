namespace Genbox.FastData.Benchmarks.Benchmarks;

public class NumericEarlyExitBenchmarks
{
    private int _min = 3;
    private int _max = 42;
    private int _value = 7;

    [Benchmark]public bool ValueRange() => _value < _min || _value > _max;
    [Benchmark]public bool ValueBitMask() => (_value & 29) != 0;
}