using System.Diagnostics.CodeAnalysis;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[DisassemblyDiagnoser]
[SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
public class NumericEarlyExitBenchmarks
{
    private int _min = 3;
    private int _max = 42;
    private int _value = 7;
    private byte _byteMin = 42;
    private byte _byteMax = 100;
    private byte _byteValue = 41;
    private sbyte _sbyteMin = -10;
    private sbyte _sbyteMax = 20;
    private sbyte _sbyteValue = -11;
    private Vector256<int> _simdSet = Vector256.Create(3, 5, 7, 11, 13, 17, 19, 23);

    [Benchmark]public bool ValueRange() => _value < _min || _value > _max;

    [Benchmark]public bool ValueRangeReduced() => _value - _min > _max - _min;

    [Benchmark]public bool ValueRangeReducedUnsigned() => (uint)(_value - _min) > (uint)(_max - _min);

    [Benchmark]public bool ByteValueRange() => _byteValue < _byteMin || _byteValue > _byteMax;

    [Benchmark]public bool ByteValueRangeReducedMasked() => (unchecked((uint)_byteValue - _byteMin) & byte.MaxValue) > _byteMax - _byteMin;

    [Benchmark]public bool ByteValueRangeReducedUnsigned() => unchecked((uint)_byteValue - _byteMin) > _byteMax - _byteMin;

    [Benchmark]public bool SByteValueRange() => _sbyteValue < _sbyteMin || _sbyteValue > _sbyteMax;

    [Benchmark]public bool SByteValueRangeReducedMasked() => (unchecked((uint)_sbyteValue - (uint)_sbyteMin) & byte.MaxValue) > _sbyteMax - _sbyteMin;

    [Benchmark]public bool SByteValueRangeReducedUnsigned() => unchecked((uint)_sbyteValue - (uint)_sbyteMin) > _sbyteMax - _sbyteMin;

    [Benchmark]public bool ValueBitMask() => (_value & 29) != 0;

    [Benchmark]public bool ValueSimd256()
    {
        Vector256<int> value = Vector256.Create(_value);
        Vector256<int> matches = Avx2.CompareEqual(value, _simdSet);
        return Avx2.MoveMask(matches.AsByte()) != 0;
    }
}