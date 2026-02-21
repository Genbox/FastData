namespace Genbox.FastData.Benchmarks.Benchmarks;

[DisassemblyDiagnoser]
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

    [Benchmark]public bool LengthEqualOpti()
    {
        int length = _value.Length;
        return length != 49;
    }

    [Benchmark]public bool LengthRange() => _value.Length < _min || _value.Length > _max;

    [Benchmark]public bool LengthRangeOpti()
    {
        int length = _value.Length;
        return (uint)(length - _min) > (uint)(_max - _min);
    }

    [Benchmark]public bool LengthBitmap() => (_bitset[_value.Length >> 6] & (1UL << ((_value.Length - 1) & 63))) == 0;

    [Benchmark]public bool LengthBitmapOpti()
    {
        int length = _value.Length;
        return (_bitset[length >> 6] & (1UL << ((length - 1) & 63))) == 0;
    }

    [Benchmark]public bool LengthDivisor() => _value.Length % 3 != 0;

    [Benchmark]public bool LengthDivisorOpti()
    {
        int length = _value.Length;
        return length % 3 != 0;
    }

    [Benchmark]public bool CharEqualsFirst() => _value[0] != 'h';

    [Benchmark]public bool CharEqualsFirstOpti()
    {
        string value = _value;
        return value.Length == 0 || value[0] != 'h';
    }

    [Benchmark]public bool CharEqualsLast() => _value[^1] != 'h';

    [Benchmark]public bool CharEqualsLastOpti()
    {
        string value = _value;
        int length = value.Length;
        return length == 0 || value[length - 1] != 'h';
    }

    [Benchmark]public bool CharEqualsFirstLast() => _value[0] != 'h' || _value[^1] != 'd';

    [Benchmark]public bool CharEqualsFirstLastOpti()
    {
        string value = _value;
        int length = value.Length;
        if (length == 0)
            return true;

        if (value[0] != 'h')
            return true;

        return value[length - 1] != 'd';
    }

    [Benchmark]public bool CharRangeFirst() => _value[0] < _min || _value[0] > _max;

    [Benchmark]public bool CharRangeFirstOpti()
    {
        string value = _value;
        if (value.Length == 0)
            return true;

        int ch = value[0];
        return (uint)(ch - _min) > (uint)(_max - _min);
    }

    [Benchmark]public bool CharRangeLast() => _value[^1] < _min || _value[^1] > _max;

    [Benchmark]public bool CharRangeLastOpti()
    {
        string value = _value;
        int length = value.Length;
        if (length == 0)
            return true;

        int ch = value[length - 1];
        return (uint)(ch - _min) > (uint)(_max - _min);
    }

    [Benchmark]public bool CharBitmapFirst() => ((1UL << _value[0]) & _firstLow) == 0;

    [Benchmark]public bool CharBitmapFirstOpti()
    {
        string value = _value;
        if (value.Length == 0)
            return true;

        int ch = value[0];
        return ((1UL << ch) & _firstLow) == 0;
    }

    [Benchmark]public bool CharBitmapLast() => ((1UL << _value[^1]) & _lastLow) == 0;

    [Benchmark]public bool CharBitmapLastOpti()
    {
        string value = _value;
        int length = value.Length;
        if (length == 0)
            return true;

        int ch = value[length - 1];
        return ((1UL << ch) & _lastLow) == 0;
    }

    [Benchmark]public bool StringBitMask() => ((_value[0] | ((ulong)_value[1] << 16) | ((ulong)_value[2] << 32) | ((ulong)_value[3] << 48)) & _stringMask) != 0;

    [Benchmark]public bool StringBitMaskOpti()
    {
        string value = _value;
        if (value.Length < 4)
            return true;

        ulong packed = value[0] | ((ulong)value[1] << 16) | ((ulong)value[2] << 32) | ((ulong)value[3] << 48);
        return (packed & _stringMask) != 0;
    }

    [Benchmark]public bool PrefixSuffix() => _value.StartsWith(_prefix, StringComparison.Ordinal) && _value.EndsWith(_suffix, StringComparison.Ordinal);

    [Benchmark]public bool PrefixSuffixOpti()
    {
        string value = _value;
        string prefix = _prefix;
        string suffix = _suffix;
        int length = value.Length;
        int prefixLength = prefix.Length;
        int suffixLength = suffix.Length;

        if (length < prefixLength || length < suffixLength)
            return false;

        for (int i = 0; i < prefixLength; i++)
        {
            if (value[i] != prefix[i])
                return false;
        }

        int suffixStart = length - suffixLength;
        for (int i = 0; i < suffixLength; i++)
        {
            if (value[suffixStart + i] != suffix[i])
                return false;
        }

        return true;
    }
}