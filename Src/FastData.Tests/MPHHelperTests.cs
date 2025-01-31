using System.Diagnostics;
using Genbox.FastHash;
using Genbox.FastData.Internal;
using Genbox.FastData.Internal.Helpers;
using Genbox.FastData.InternalShared;
using Genbox.FastData.InternalShared.Helpers;

namespace Genbox.FastData.Tests;

public class MPHHelperTests
{
    private long _time;
    private const int _numSeconds = 10;

    [Fact]
    public void MinimalPerfectHashTest()
    {
        uint[] data = StringHelper.GetIntegers(TestData.Words.Take(10));

        uint seed = MPHHelper.Generate(data, static (a, b) => MixFunctions.Murmur_32_Seed(a, b), 1).First();
        Assert.True(MPHHelper.Validate(data, seed, static (a, b) => MixFunctions.Murmur_32_Seed(a, b), out byte[] _));
    }

    [Fact]
    public void PerfectHashTest()
    {
        uint[] data = StringHelper.GetIntegers(TestData.Words.Take(10));

        uint seed = MPHHelper.Generate(data, static (a, b) => MixFunctions.Murmur_32_Seed(a, b), 1, uint.MaxValue, 64).First();
        Assert.True(MPHHelper.Validate(data, seed, static (a, b) => MixFunctions.Murmur_32_Seed(a, b), out byte[] _, 64));
    }

    [Fact]
    public void ConditionTest()
    {
        uint[] data = StringHelper.GetIntegers(TestData.Words.Take(100));

        _time = Stopwatch.GetTimestamp();
        uint[] seed = MPHHelper.Generate(data, static (a, b) => MixFunctions.Murmur_32_Seed(a, b), 1, uint.MaxValue, 0, Condition).ToArray();

        Assert.Empty(seed);
        Assert.Equal(_numSeconds, (int)Stopwatch.GetElapsedTime(_time).TotalSeconds);
    }

    private bool Condition() => Stopwatch.GetElapsedTime(_time).TotalSeconds > _numSeconds;
}