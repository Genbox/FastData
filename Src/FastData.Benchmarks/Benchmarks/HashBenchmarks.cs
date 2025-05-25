using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Order;
using Genbox.FastData.Internal.Hashes;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class HashBenchmarks
{
    private string[] _array;

    [Params(1_000, 10_000, 100_000, 1_000_000)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _array = Enumerable.Range(0, Size).Select(x => x.ToString(NumberFormatInfo.InvariantInfo)).ToArray();
    }

    [Benchmark]
    public ulong DJB2HashTest()
    {
        ulong value = 0;

        foreach (string s in _array)
        {
            ref char ptr = ref MemoryMarshal.GetReference(s.AsSpan());
            ref byte ptr2 = ref Unsafe.As<char, byte>(ref ptr);
            value += DJB2Hash.ComputeHash(ref ptr2, s.Length);
        }

        return value;
    }

    [Benchmark]
    public ulong XXHashTest()
    {
        ulong value = 0;

        foreach (string s in _array)
            value += XxHash.ComputeHash(s);

        return value;
    }
}