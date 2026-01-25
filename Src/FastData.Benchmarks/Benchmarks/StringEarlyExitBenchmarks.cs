namespace Genbox.FastData.Benchmarks.Benchmarks;

public class StringEarlyExitBenchmarks
{
    private string _value = "hello world";
    private int _min = 3;
    private int _max = 42;
    private string _prefix = "he";
    private string _suffix = "ld";
    private ulong[] _bitset = [2828, 4848];
    private ulong _firstLow = 60;
    private ulong _lastLow = 60;
    private ulong _stringMask = 0xFF93FF93FF9AFF97UL;

    [Benchmark]public bool LengthEqual() => _value.Length != 49;
    [Benchmark]public bool LengthRange() => _value.Length < _min || _value.Length > _max;
    [Benchmark]public bool LengthBitmap() => (_bitset[_value.Length >> 6] & (1UL << ((_value.Length - 1) & 63))) == 0;
    [Benchmark]public bool LengthDivisor() => _value.Length % 3 != 0;
    [Benchmark]public bool CharEqualsFirst() => _value[0] != 'h';
    [Benchmark]public bool CharEqualsLast() => _value[^1] != 'h';
    [Benchmark]public bool CharEqualsFirstLast() => _value[0] != 'h' || _value[^1] != 'd';
    [Benchmark]public bool CharRangeFirst() => _value[0] < _min || _value[0] > _max;
    [Benchmark]public bool CharRangeLast() => _value[^1] < _min || _value[^1] > _max;
    [Benchmark]public bool CharBitmapFirst() => ((1UL << _value[0]) & _firstLow) == 0;
    [Benchmark]public bool CharBitmapLast() => ((1UL << _value[^1]) & _lastLow) == 0;
    [Benchmark]public bool StringBitMask() => ((_value[0] | ((ulong)_value[1] << 16) | ((ulong)_value[2] << 32) | ((ulong)_value[3] << 48)) & _stringMask) != 0;
    [Benchmark]public bool PrefixSuffix() => _value.StartsWith(_prefix, StringComparison.Ordinal) && _value.EndsWith(_suffix, StringComparison.Ordinal);
}