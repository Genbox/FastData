using System.Diagnostics;
using Genbox.FastData.InternalShared;
using static Genbox.FastData.Internal.Helpers.PerfectHashHelper;
using static Genbox.FastData.InternalShared.TestHelper;

namespace Genbox.FastData.Tests;

public class PerfectHashHelperTests
{
    private const int _numSeconds = 3;
    private long _time;

    [Fact]
    public void MinimalPerfectHashTest()
    {
        uint[] data = GetIntegers(TestData.Words.Take(10));

        uint seed = Generate(data, static (obj, seed) => Mixers.Murmur_32((uint)obj ^ seed), 1).First();
        Assert.True(Validate(data, seed, static (obj, seed) => Mixers.Murmur_32((uint)obj ^ seed), out byte[] _));
    }

    [Fact]
    public void PerfectHashTest()
    {
        uint[] data = GetIntegers(TestData.Words.Take(10));

        uint seed = Generate(data, static (obj, seed) => Mixers.Murmur_32((uint)obj ^ seed), 1, uint.MaxValue, 64).First();
        Assert.True(Validate(data, seed, static (obj, seed) => Mixers.Murmur_32((uint)obj ^ seed), out byte[] _, 64));
    }

    [Fact]
    public void ConditionTest()
    {
        uint[] data = GetIntegers(TestData.Words.Take(100));

        _time = Stopwatch.GetTimestamp();
        uint[] seed = Generate(data, static (obj, seed) => Mixers.Murmur_32((uint)obj ^ seed), 1, uint.MaxValue, 0, Condition).ToArray();

        Assert.Empty(seed);
        Assert.Equal(_numSeconds, (int)Stopwatch.GetElapsedTime(_time).TotalSeconds);
    }

    private bool Condition() => Stopwatch.GetElapsedTime(_time).TotalSeconds > _numSeconds;
}