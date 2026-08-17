using BenchmarkDotNet.Order;
using Genbox.FastData.Internal;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class DeduplicationDispatchBenchmarks
{
    private byte[] _byteKeys = null!;
    private ushort[] _uint16Keys = null!;

    [Params(1025, 16384)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _byteKeys = new byte[Count];
        _uint16Keys = new ushort[Count];

        for (int i = 0; i < Count; i++)
        {
            int value = i * 997 % 200;
            _byteKeys[i] = (byte)value;
            _uint16Keys[i] = (ushort)(10000 + value);
        }
    }

    [Benchmark]
    public int Byte()
    {
        byte[] keys = (byte[])_byteKeys.Clone();
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [Benchmark]
    public int UInt16()
    {
        ushort[] keys = (ushort[])_uint16Keys.Clone();
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }
}