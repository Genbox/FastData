using System.Diagnostics;
using System.Runtime.CompilerServices;
using Genbox.FastData.InternalShared;
using static Genbox.FastData.Internal.Helpers.MPHHelper;
using static Genbox.FastData.InternalShared.StringHelper;

namespace Genbox.FastData.Tests;

public class MPHHelperTests
{
    private long _time;
    private const int _numSeconds = 10;

    [Fact]
    public void MinimalPerfectHashTest()
    {
        uint[] data = GetIntegers(TestData.Words.Take(10));

        uint seed = Generate(data, static (a, b) => MurMurSeed(a, b), 1).First();
        Assert.True(Validate(data, seed, static (a, b) => MurMurSeed(a, b), out byte[] _));
    }

    [Fact]
    public void PerfectHashTest()
    {
        uint[] data = GetIntegers(TestData.Words.Take(10));

        uint seed = Generate(data, static (a, b) => MurMurSeed(a, b), 1, uint.MaxValue, 64).First();
        Assert.True(Validate(data, seed, static (a, b) => MurMurSeed(a, b), out byte[] _, 64));
    }

    [Fact]
    public void ConditionTest()
    {
        uint[] data = GetIntegers(TestData.Words.Take(100));

        _time = Stopwatch.GetTimestamp();
        uint[] seed = Generate(data, static (a, b) => MurMurSeed(a, b), 1, uint.MaxValue, 0, Condition).ToArray();

        Assert.Empty(seed);
        Assert.Equal(_numSeconds, (int)Stopwatch.GetElapsedTime(_time).TotalSeconds);
    }

    private bool Condition() => Stopwatch.GetElapsedTime(_time).TotalSeconds > _numSeconds;

    private static uint MurMurSeed(uint a, uint b) => unchecked(AA_xmxmx_Murmur_32(a + b));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static uint AA_xmxmx_Murmur_32(uint h)
    {
        unchecked
        {
            h ^= h >> 16;
            h *= 0x85EBCA6B;
            h ^= h >> 13;
            h *= 0xC2B2AE35;
            h ^= h >> 16;
            return h;
        }
    }
}