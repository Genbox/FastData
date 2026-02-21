using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[DisassemblyDiagnoser]
public class NumericEarlyExitBenchmarks
{
    private int _min = 3;
    private int _max = 42;
    private int _value = 7;
    private Vector256<int> _simdSet = Vector256.Create(3, 5, 7, 11, 13, 17, 19, 23);

    [Benchmark]public bool ValueRange() => _value < _min || _value > _max;

    [Benchmark]public bool ValueRangeReduced() => _value - _min > _max - _min;

    [Benchmark]public bool ValueRangeReducedUnsigned() => (uint)(_value - _min) > (uint)(_max - _min);

    [Benchmark]public bool ValueBitMask() => (_value & 29) != 0;

    [Benchmark]public bool ValueSimd256()
    {
        Vector256<int> value = Vector256.Create(_value);
        Vector256<int> matches = Avx2.CompareEqual(value, _simdSet);
        return Avx2.MoveMask(matches.AsByte()) != 0;
    }
}