using BenchmarkDotNet.Configs;
using Genbox.FastData.Internal;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[HideColumns("Gen0", "Gen1", "Gen2")]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class DeduplicationBenchmarks
{
    private const int RangeCount = 1000;
    private const int RangeSize = 200;
    private const int BitSetCount = 16_384;
    private const int BitSetRangeStep = 97;

    private byte[] _rangeUInt8Keys = null!;
    private sbyte[] _rangeInt8Keys = null!;
    private char[] _rangeCharKeys = null!;
    private short[] _rangeInt16Keys = null!;
    private ushort[] _rangeUInt16Keys = null!;
    private int[] _rangeInt32Keys = null!;
    private uint[] _rangeUInt32Keys = null!;
    private long[] _rangeInt64Keys = null!;
    private ulong[] _rangeUInt64Keys = null!;
    private int[] _bitSetInt32Keys = null!;
    private uint[] _bitSetUInt32Keys = null!;
    private long[] _bitSetInt64Keys = null!;
    private ulong[] _bitSetUInt64Keys = null!;

    [GlobalSetup]
    public void Setup()
    {
        _rangeUInt8Keys = new byte[RangeCount];
        _rangeInt8Keys = new sbyte[RangeCount];
        _rangeCharKeys = new char[RangeCount];
        _rangeInt16Keys = new short[RangeCount];
        _rangeUInt16Keys = new ushort[RangeCount];
        _rangeInt32Keys = new int[RangeCount];
        _rangeUInt32Keys = new uint[RangeCount];
        _rangeInt64Keys = new long[RangeCount];
        _rangeUInt64Keys = new ulong[RangeCount];
        _bitSetInt32Keys = new int[BitSetCount];
        _bitSetUInt32Keys = new uint[BitSetCount];
        _bitSetInt64Keys = new long[BitSetCount];
        _bitSetUInt64Keys = new ulong[BitSetCount];

        for (int i = 0; i < RangeCount; i++)
        {
            int value = i * 997 % RangeSize;
            _rangeUInt8Keys[i] = (byte)value;
            _rangeInt8Keys[i] = (sbyte)(value - 100);
            _rangeCharKeys[i] = (char)value;
            _rangeInt16Keys[i] = (short)(value - 100);
            _rangeUInt16Keys[i] = (ushort)value;
            _rangeInt32Keys[i] = value - 100;
            _rangeUInt32Keys[i] = (uint)value;
            _rangeInt64Keys[i] = value - 100L;
            _rangeUInt64Keys[i] = (ulong)value;
        }

        for (int i = 0; i < BitSetCount; i++)
        {
            int value = i % (BitSetCount / 2) * BitSetRangeStep;
            _bitSetInt32Keys[i] = value - 100_000;
            _bitSetUInt32Keys[i] = (uint)value + 1_000_000u;
            _bitSetInt64Keys[i] = value - 100_000L;
            _bitSetUInt64Keys[i] = (ulong)value + 1_000_000ul;
        }
    }

    [BenchmarkCategory("UInt8Range"), Benchmark(Baseline = true)]
    public int BaselineUInt8Range()
    {
        byte[] keys = Copy(_rangeUInt8Keys);
        return BaselineDeduplicate(keys, EqualityComparer<byte>.Default, Comparer<byte>.Default);
    }

    [BenchmarkCategory("UInt8Range"), Benchmark]
    public int FastDataUInt8Range()
    {
        byte[] keys = Copy(_rangeUInt8Keys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("Int8Range"), Benchmark(Baseline = true)]
    public int BaselineInt8Range()
    {
        sbyte[] keys = Copy(_rangeInt8Keys);
        return BaselineDeduplicate(keys, EqualityComparer<sbyte>.Default, Comparer<sbyte>.Default);
    }

    [BenchmarkCategory("Int8Range"), Benchmark]
    public int FastDataInt8Range()
    {
        sbyte[] keys = Copy(_rangeInt8Keys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("CharRange"), Benchmark(Baseline = true)]
    public int BaselineCharRange()
    {
        char[] keys = Copy(_rangeCharKeys);
        return BaselineDeduplicate(keys, EqualityComparer<char>.Default, Comparer<char>.Default);
    }

    [BenchmarkCategory("CharRange"), Benchmark]
    public int FastDataCharRange()
    {
        char[] keys = Copy(_rangeCharKeys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("Int16Range"), Benchmark(Baseline = true)]
    public int BaselineInt16Range()
    {
        short[] keys = Copy(_rangeInt16Keys);
        return BaselineDeduplicate(keys, EqualityComparer<short>.Default, Comparer<short>.Default);
    }

    [BenchmarkCategory("Int16Range"), Benchmark]
    public int FastDataInt16Range()
    {
        short[] keys = Copy(_rangeInt16Keys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("UInt16Range"), Benchmark(Baseline = true)]
    public int BaselineUInt16Range()
    {
        ushort[] keys = Copy(_rangeUInt16Keys);
        return BaselineDeduplicate(keys, EqualityComparer<ushort>.Default, Comparer<ushort>.Default);
    }

    [BenchmarkCategory("UInt16Range"), Benchmark]
    public int FastDataUInt16Range()
    {
        ushort[] keys = Copy(_rangeUInt16Keys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("Int32Range"), Benchmark(Baseline = true)]
    public int BaselineInt32Range()
    {
        int[] keys = Copy(_rangeInt32Keys);
        return BaselineDeduplicate(keys, EqualityComparer<int>.Default, Comparer<int>.Default);
    }

    [BenchmarkCategory("Int32Range"), Benchmark]
    public int FastDataInt32Range()
    {
        int[] keys = Copy(_rangeInt32Keys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("UInt32Range"), Benchmark(Baseline = true)]
    public int BaselineUInt32Range()
    {
        uint[] keys = Copy(_rangeUInt32Keys);
        return BaselineDeduplicate(keys, EqualityComparer<uint>.Default, Comparer<uint>.Default);
    }

    [BenchmarkCategory("UInt32Range"), Benchmark]
    public int FastDataUInt32Range()
    {
        uint[] keys = Copy(_rangeUInt32Keys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("Int64Range"), Benchmark(Baseline = true)]
    public int BaselineInt64Range()
    {
        long[] keys = Copy(_rangeInt64Keys);
        return BaselineDeduplicate(keys, EqualityComparer<long>.Default, Comparer<long>.Default);
    }

    [BenchmarkCategory("Int64Range"), Benchmark]
    public int FastDataInt64Range()
    {
        long[] keys = Copy(_rangeInt64Keys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("UInt64Range"), Benchmark(Baseline = true)]
    public int BaselineUInt64Range()
    {
        ulong[] keys = Copy(_rangeUInt64Keys);
        return BaselineDeduplicate(keys, EqualityComparer<ulong>.Default, Comparer<ulong>.Default);
    }

    [BenchmarkCategory("UInt64Range"), Benchmark]
    public int FastDataUInt64Range()
    {
        ulong[] keys = Copy(_rangeUInt64Keys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("Int32BitSet"), Benchmark(Baseline = true)]
    public int BaselineInt32BitSet()
    {
        int[] keys = Copy(_bitSetInt32Keys);
        return BaselineDeduplicate(keys, EqualityComparer<int>.Default, Comparer<int>.Default);
    }

    [BenchmarkCategory("Int32BitSet"), Benchmark]
    public int FastDataInt32BitSet()
    {
        int[] keys = Copy(_bitSetInt32Keys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("UInt32BitSet"), Benchmark(Baseline = true)]
    public int BaselineUInt32BitSet()
    {
        uint[] keys = Copy(_bitSetUInt32Keys);
        return BaselineDeduplicate(keys, EqualityComparer<uint>.Default, Comparer<uint>.Default);
    }

    [BenchmarkCategory("UInt32BitSet"), Benchmark]
    public int FastDataUInt32BitSet()
    {
        uint[] keys = Copy(_bitSetUInt32Keys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("Int64BitSet"), Benchmark(Baseline = true)]
    public int BaselineInt64BitSet()
    {
        long[] keys = Copy(_bitSetInt64Keys);
        return BaselineDeduplicate(keys, EqualityComparer<long>.Default, Comparer<long>.Default);
    }

    [BenchmarkCategory("Int64BitSet"), Benchmark]
    public int FastDataInt64BitSet()
    {
        long[] keys = Copy(_bitSetInt64Keys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("UInt64BitSet"), Benchmark(Baseline = true)]
    public int BaselineUInt64BitSet()
    {
        ulong[] keys = Copy(_bitSetUInt64Keys);
        return BaselineDeduplicate(keys, EqualityComparer<ulong>.Default, Comparer<ulong>.Default);
    }

    [BenchmarkCategory("UInt64BitSet"), Benchmark]
    public int FastDataUInt64BitSet()
    {
        ulong[] keys = Copy(_bitSetUInt64Keys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    private static int BaselineDeduplicate<TKey>(TKey[] keys, IEqualityComparer<TKey> equalityComparer, IComparer<TKey> sortComparer)
    {
        Array.Sort(keys, sortComparer);

        if (keys.Length is 0 or 1)
            return keys.Length;

        TKey current = keys[0];
        int uniqueCount = 1;

        for (int i = 1; i < keys.Length; i++)
        {
            TKey key = keys[i];

            if (equalityComparer.Equals(key, current))
                continue;

            keys[uniqueCount] = key;
            current = key;
            uniqueCount++;
        }

        return uniqueCount;
    }

    private static T[] Copy<T>(T[] source)
    {
        T[] destination = new T[source.Length];
        Array.Copy(source, destination, source.Length);
        return destination;
    }
}