using Genbox.FastData.Internal;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[MemoryDiagnoser]
public class ValueHelperBenchmarks
{
    private char[] _charKeys = null!;
    private short[] _int16Keys = null!;
    private ushort[] _uint16Keys = null!;
    private int[] _int32Keys = null!;
    private uint[] _uint32Keys = null!;
    private long[] _int64Keys = null!;
    private ulong[] _uint64Keys = null!;

    [Params(65536)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Random rng = new Random(42);

        _charKeys = new char[Count];
        _int16Keys = new short[Count];
        _uint16Keys = new ushort[Count];
        _int32Keys = new int[Count];
        _uint32Keys = new uint[Count];
        _int64Keys = new long[Count];
        _uint64Keys = new ulong[Count];

        for (int i = 0; i < Count; i++)
        {
            int unsigned16Value = rng.Next(0, ushort.MaxValue + 1);

            _charKeys[i] = (char)unsigned16Value;
            _int16Keys[i] = (short)rng.Next(short.MinValue, short.MaxValue + 1);
            _uint16Keys[i] = (ushort)unsigned16Value;
            _int32Keys[i] = rng.Next(int.MinValue, int.MaxValue);
            _uint32Keys[i] = (uint)rng.NextInt64(0, uint.MaxValue + 1L);
            _int64Keys[i] = rng.NextInt64(long.MinValue, long.MaxValue);
            _uint64Keys[i] = unchecked((ulong)rng.NextInt64(long.MinValue, long.MaxValue));
        }
    }

    [Benchmark]
    public (char Min, char Max) Char()
    {
        ValueHelper.GetCharMinMax(_charKeys, out char min, out char max);
        return (min, max);
    }

    [Benchmark]
    public (short Min, short Max) Int16()
    {
        ValueHelper.GetInt16MinMax(_int16Keys, out short min, out short max);
        return (min, max);
    }

    [Benchmark]
    public (ushort Min, ushort Max) UInt16()
    {
        ValueHelper.GetUInt16MinMax(_uint16Keys, out ushort min, out ushort max);
        return (min, max);
    }

    [Benchmark]
    public (int Min, int Max) Int32()
    {
        ValueHelper.GetInt32MinMax(_int32Keys, out int min, out int max);
        return (min, max);
    }

    [Benchmark]
    public (uint Min, uint Max) UInt32()
    {
        ValueHelper.GetUInt32MinMax(_uint32Keys, out uint min, out uint max);
        return (min, max);
    }

    [Benchmark]
    public (long Min, long Max) Int64()
    {
        ValueHelper.GetInt64MinMax(_int64Keys, out long min, out long max);
        return (min, max);
    }

    [Benchmark]
    public (ulong Min, ulong Max) UInt64()
    {
        ValueHelper.GetUInt64MinMax(_uint64Keys, out ulong min, out ulong max);
        return (min, max);
    }
}