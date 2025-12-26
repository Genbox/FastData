namespace Genbox.FastData.Benchmarks.Benchmarks;

/// <summary>Benchmarks where the threshold usually is between a full hash/lookup in a HashSet vs. how long a prefix/suffix early exit can be before it no longer makes sense</summary>
public class EarlyExitVsLookup
{
    private static string _myStr = "hello world";

    private static readonly HashSet<string> _hashSet = new HashSet<string>(StringComparer.Ordinal)
    {
        _myStr
    };

    [Benchmark(Baseline = true)]
    public bool Lookup() => _hashSet.Contains(_myStr);

    [Benchmark]
    public bool Size1() => _myStr.StartsWith('h');

    [Benchmark]
    public bool Size2() => _myStr.StartsWith("he", StringComparison.Ordinal);

    [Benchmark]
    public bool Size3() => _myStr.StartsWith("hel", StringComparison.Ordinal);

    [Benchmark]
    public bool Size4() => _myStr.StartsWith("hell", StringComparison.Ordinal);

    [Benchmark]
    public bool Size5() => _myStr.StartsWith("hello", StringComparison.Ordinal);

    [Benchmark]
    public bool Size6() => _myStr.StartsWith("hello ", StringComparison.Ordinal);

    [Benchmark]
    public bool Size7() => _myStr.StartsWith("hello w", StringComparison.Ordinal);

    [Benchmark]
    public bool Size8() => _myStr.StartsWith("hello wo", StringComparison.Ordinal);

    [Benchmark]
    public bool Size9() => _myStr.StartsWith("hello wor", StringComparison.Ordinal);

    [Benchmark]
    public bool Size10() => _myStr.StartsWith("hello worl", StringComparison.Ordinal);

    [Benchmark]
    public bool Size11() => _myStr.StartsWith("hello world", StringComparison.Ordinal);
}