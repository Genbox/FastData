using BenchmarkDotNet.Configs;
using Genbox.FastData.Internal;

namespace Genbox.FastData.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
public class DeduplicationBenchmarks
{
    private const int Count = 1000;

    private byte[] _byteKeys = null!;
    private char[] _charKeys = null!;
    private int[] _intKeys = null!;
    private int[] _intValues = null!;
    private int[] _int32AllDuplicateKeys = null!;
    private int[] _int32AllUniqueKeys = null!;
    private int[] _int32HalfDuplicateKeys = null!;
    private int[] _int32RandomSparseKeys = null!;
    private int[] _int32ReverseSortedKeys = null!;
    private long[] _longKeys = null!;
    private sbyte[] _sbyteKeys = null!;
    private char[] _sortedCharKeys = null!;
    private short[] _sortedInt16Keys = null!;
    private int[] _sortedInt32Keys = null!;
    private long[] _sortedInt64Keys = null!;
    private ushort[] _sortedUInt16Keys = null!;
    private uint[] _sortedUInt32Keys = null!;
    private ulong[] _sortedUInt64Keys = null!;
    private short[] _shortKeys = null!;
    private uint[] _uintKeys = null!;
    private ulong[] _ulongKeys = null!;
    private ushort[] _ushortKeys = null!;

    [GlobalSetup]
    public void Setup()
    {
        Random rng = new Random(42);

        _byteKeys = new byte[Count];
        _sbyteKeys = new sbyte[Count];
        _charKeys = new char[Count];
        _shortKeys = new short[Count];
        _ushortKeys = new ushort[Count];
        _intKeys = new int[Count];
        _uintKeys = new uint[Count];
        _longKeys = new long[Count];
        _ulongKeys = new ulong[Count];
        _intValues = new int[Count];
        _int32AllDuplicateKeys = new int[Count];
        _int32AllUniqueKeys = new int[Count];
        _int32HalfDuplicateKeys = new int[Count];
        _int32RandomSparseKeys = new int[Count];
        _int32ReverseSortedKeys = new int[Count];
        _sortedCharKeys = new char[Count];
        _sortedInt16Keys = new short[Count];
        _sortedUInt16Keys = new ushort[Count];
        _sortedInt32Keys = new int[Count];
        _sortedUInt32Keys = new uint[Count];
        _sortedInt64Keys = new long[Count];
        _sortedUInt64Keys = new ulong[Count];

        for (int i = 0; i < Count; i++)
        {
            int value = rng.Next(0, 200);
            _byteKeys[i] = (byte)value;
            _sbyteKeys[i] = (sbyte)rng.Next(-100, 100);
            _charKeys[i] = (char)value;
            _shortKeys[i] = (short)rng.Next(-100, 100);
            _ushortKeys[i] = (ushort)value;
            _intKeys[i] = rng.Next(-100, 100);
            _uintKeys[i] = (uint)value;
            _longKeys[i] = rng.NextInt64(-100, 100);
            _ulongKeys[i] = (ulong)value;
            _intValues[i] = i;

            int sortedValue = i / 2;
            _sortedCharKeys[i] = (char)sortedValue;
            _sortedInt16Keys[i] = (short)(sortedValue - 250);
            _sortedUInt16Keys[i] = (ushort)sortedValue;
            _sortedInt32Keys[i] = sortedValue - 250;
            _sortedUInt32Keys[i] = (uint)sortedValue;
            _sortedInt64Keys[i] = sortedValue - 250L;
            _sortedUInt64Keys[i] = (ulong)sortedValue;

            _int32AllDuplicateKeys[i] = 42;
            _int32AllUniqueKeys[i] = i;
            _int32HalfDuplicateKeys[i] = i * 997 % (Count / 2);
            _int32RandomSparseKeys[i] = rng.Next(0, Count * 1000);
            _int32ReverseSortedKeys[i] = Count - i - 1;
        }
    }

    [BenchmarkCategory("ByteRandomDense"), Benchmark(Baseline = true)]
    public int GenericByteRandomDense()
    {
        byte[] keys = Copy(_byteKeys);
        return GenericDeduplicateWithSort(keys, EqualityComparer<byte>.Default, Comparer<byte>.Default);
    }

    [BenchmarkCategory("ByteRandomDense"), Benchmark]
    public int FastDataByteRandomDense()
    {
        byte[] keys = Copy(_byteKeys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("SByteRandomDense"), Benchmark(Baseline = true)]
    public int GenericSByteRandomDense()
    {
        sbyte[] keys = Copy(_sbyteKeys);
        return GenericDeduplicateWithSort(keys, EqualityComparer<sbyte>.Default, Comparer<sbyte>.Default);
    }

    [BenchmarkCategory("SByteRandomDense"), Benchmark]
    public int FastDataSByteRandomDense()
    {
        sbyte[] keys = Copy(_sbyteKeys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("CharRandomDense"), Benchmark(Baseline = true)]
    public int GenericCharRandomDense()
    {
        char[] keys = Copy(_charKeys);
        return GenericDeduplicateWithSort(keys, EqualityComparer<char>.Default, Comparer<char>.Default);
    }

    [BenchmarkCategory("CharRandomDense"), Benchmark]
    public int FastDataCharRandomDense()
    {
        char[] keys = Copy(_charKeys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("Int16RandomDense"), Benchmark(Baseline = true)]
    public int GenericInt16RandomDense()
    {
        short[] keys = Copy(_shortKeys);
        return GenericDeduplicateWithSort(keys, EqualityComparer<short>.Default, Comparer<short>.Default);
    }

    [BenchmarkCategory("Int16RandomDense"), Benchmark]
    public int FastDataInt16RandomDense()
    {
        short[] keys = Copy(_shortKeys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("UInt16RandomDense"), Benchmark(Baseline = true)]
    public int GenericUInt16RandomDense()
    {
        ushort[] keys = Copy(_ushortKeys);
        return GenericDeduplicateWithSort(keys, EqualityComparer<ushort>.Default, Comparer<ushort>.Default);
    }

    [BenchmarkCategory("UInt16RandomDense"), Benchmark]
    public int FastDataUInt16RandomDense()
    {
        ushort[] keys = Copy(_ushortKeys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("UInt32RandomDense"), Benchmark(Baseline = true)]
    public int GenericUInt32RandomDense()
    {
        uint[] keys = Copy(_uintKeys);
        return GenericDeduplicateWithSort(keys, EqualityComparer<uint>.Default, Comparer<uint>.Default);
    }

    [BenchmarkCategory("UInt32RandomDense"), Benchmark]
    public int FastDataUInt32RandomDense()
    {
        uint[] keys = Copy(_uintKeys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("Int64RandomDense"), Benchmark(Baseline = true)]
    public int GenericInt64RandomDense()
    {
        long[] keys = Copy(_longKeys);
        return GenericDeduplicateWithSort(keys, EqualityComparer<long>.Default, Comparer<long>.Default);
    }

    [BenchmarkCategory("Int64RandomDense"), Benchmark]
    public int FastDataInt64RandomDense()
    {
        long[] keys = Copy(_longKeys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("UInt64RandomDense"), Benchmark(Baseline = true)]
    public int GenericUInt64RandomDense()
    {
        ulong[] keys = Copy(_ulongKeys);
        return GenericDeduplicateWithSort(keys, EqualityComparer<ulong>.Default, Comparer<ulong>.Default);
    }

    [BenchmarkCategory("UInt64RandomDense"), Benchmark]
    public int FastDataUInt64RandomDense()
    {
        ulong[] keys = Copy(_ulongKeys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("Int32RandomDense"), Benchmark(Baseline = true)]
    public int GenericInt32RandomDense()
    {
        int[] keys = Copy(_intKeys);
        return GenericDeduplicateWithSort(keys, EqualityComparer<int>.Default, Comparer<int>.Default);
    }

    [BenchmarkCategory("Int32RandomDense"), Benchmark]
    public int FastDataInt32RandomDense()
    {
        int[] keys = Copy(_intKeys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("Int32SortedDuplicates"), Benchmark(Baseline = true)]
    public int GenericInt32SortedDuplicates()
    {
        int[] keys = Copy(_sortedInt32Keys);
        return GenericDeduplicateWithSort(keys, EqualityComparer<int>.Default, Comparer<int>.Default);
    }

    [BenchmarkCategory("Int32SortedDuplicates"), Benchmark]
    public int FastDataInt32SortedDuplicates()
    {
        int[] keys = Copy(_sortedInt32Keys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("Int32ReverseSorted"), Benchmark(Baseline = true)]
    public int GenericInt32ReverseSorted()
    {
        int[] keys = Copy(_int32ReverseSortedKeys);
        return GenericDeduplicateWithSort(keys, EqualityComparer<int>.Default, Comparer<int>.Default);
    }

    [BenchmarkCategory("Int32ReverseSorted"), Benchmark]
    public int FastDataInt32ReverseSorted()
    {
        int[] keys = Copy(_int32ReverseSortedKeys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("Int32RandomSparse"), Benchmark(Baseline = true)]
    public int GenericInt32RandomSparse()
    {
        int[] keys = Copy(_int32RandomSparseKeys);
        return GenericDeduplicateWithSort(keys, EqualityComparer<int>.Default, Comparer<int>.Default);
    }

    [BenchmarkCategory("Int32RandomSparse"), Benchmark]
    public int FastDataInt32RandomSparse()
    {
        int[] keys = Copy(_int32RandomSparseKeys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("Int32AllUnique"), Benchmark(Baseline = true)]
    public int GenericInt32AllUnique()
    {
        int[] keys = Copy(_int32AllUniqueKeys);
        return GenericDeduplicateWithSort(keys, EqualityComparer<int>.Default, Comparer<int>.Default);
    }

    [BenchmarkCategory("Int32AllUnique"), Benchmark]
    public int FastDataInt32AllUnique()
    {
        int[] keys = Copy(_int32AllUniqueKeys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("Int32AllDuplicates"), Benchmark(Baseline = true)]
    public int GenericInt32AllDuplicates()
    {
        int[] keys = Copy(_int32AllDuplicateKeys);
        return GenericDeduplicateWithSort(keys, EqualityComparer<int>.Default, Comparer<int>.Default);
    }

    [BenchmarkCategory("Int32AllDuplicates"), Benchmark]
    public int FastDataInt32AllDuplicates()
    {
        int[] keys = Copy(_int32AllDuplicateKeys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("Int32HalfDuplicates"), Benchmark(Baseline = true)]
    public int GenericInt32HalfDuplicates()
    {
        int[] keys = Copy(_int32HalfDuplicateKeys);
        return GenericDeduplicateWithSort(keys, EqualityComparer<int>.Default, Comparer<int>.Default);
    }

    [BenchmarkCategory("Int32HalfDuplicates"), Benchmark]
    public int FastDataInt32HalfDuplicates()
    {
        int[] keys = Copy(_int32HalfDuplicateKeys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("Int16RandomDenseWithValues"), Benchmark(Baseline = true)]
    public int GenericInt16RandomDenseWithValues()
    {
        short[] keys = Copy(_shortKeys);
        int[] values = Copy(_intValues);
        return GenericDeduplicateWithSort(keys, values, EqualityComparer<short>.Default, Comparer<short>.Default);
    }

    [BenchmarkCategory("Int16RandomDenseWithValues"), Benchmark]
    public int FastDataInt16RandomDenseWithValues()
    {
        short[] keys = Copy(_shortKeys);
        int[] values = Copy(_intValues);
        Deduplication.DeduplicateNumericKeysInternal(keys, values, out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("UInt16RandomDenseWithValues"), Benchmark(Baseline = true)]
    public int GenericUInt16RandomDenseWithValues()
    {
        ushort[] keys = Copy(_ushortKeys);
        int[] values = Copy(_intValues);
        return GenericDeduplicateWithSort(keys, values, EqualityComparer<ushort>.Default, Comparer<ushort>.Default);
    }

    [BenchmarkCategory("UInt16RandomDenseWithValues"), Benchmark]
    public int FastDataUInt16RandomDenseWithValues()
    {
        ushort[] keys = Copy(_ushortKeys);
        int[] values = Copy(_intValues);
        Deduplication.DeduplicateNumericKeysInternal(keys, values, out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("Int32RandomDenseWithValues"), Benchmark(Baseline = true)]
    public int GenericInt32RandomDenseWithValues()
    {
        int[] keys = Copy(_intKeys);
        int[] values = Copy(_intValues);
        return GenericDeduplicateWithSort(keys, values, EqualityComparer<int>.Default, Comparer<int>.Default);
    }

    [BenchmarkCategory("Int32RandomDenseWithValues"), Benchmark]
    public int FastDataInt32RandomDenseWithValues()
    {
        int[] keys = Copy(_intKeys);
        int[] values = Copy(_intValues);
        Deduplication.DeduplicateNumericKeysInternal(keys, values, out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("UInt32RandomDenseWithValues"), Benchmark(Baseline = true)]
    public int GenericUInt32RandomDenseWithValues()
    {
        uint[] keys = Copy(_uintKeys);
        int[] values = Copy(_intValues);
        return GenericDeduplicateWithSort(keys, values, EqualityComparer<uint>.Default, Comparer<uint>.Default);
    }

    [BenchmarkCategory("UInt32RandomDenseWithValues"), Benchmark]
    public int FastDataUInt32RandomDenseWithValues()
    {
        uint[] keys = Copy(_uintKeys);
        int[] values = Copy(_intValues);
        Deduplication.DeduplicateNumericKeysInternal(keys, values, out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("Int64RandomDenseWithValues"), Benchmark(Baseline = true)]
    public int GenericInt64RandomDenseWithValues()
    {
        long[] keys = Copy(_longKeys);
        int[] values = Copy(_intValues);
        return GenericDeduplicateWithSort(keys, values, EqualityComparer<long>.Default, Comparer<long>.Default);
    }

    [BenchmarkCategory("Int64RandomDenseWithValues"), Benchmark]
    public int FastDataInt64RandomDenseWithValues()
    {
        long[] keys = Copy(_longKeys);
        int[] values = Copy(_intValues);
        Deduplication.DeduplicateNumericKeysInternal(keys, values, out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("UInt64RandomDenseWithValues"), Benchmark(Baseline = true)]
    public int GenericUInt64RandomDenseWithValues()
    {
        ulong[] keys = Copy(_ulongKeys);
        int[] values = Copy(_intValues);
        return GenericDeduplicateWithSort(keys, values, EqualityComparer<ulong>.Default, Comparer<ulong>.Default);
    }

    [BenchmarkCategory("UInt64RandomDenseWithValues"), Benchmark]
    public int FastDataUInt64RandomDenseWithValues()
    {
        ulong[] keys = Copy(_ulongKeys);
        int[] values = Copy(_intValues);
        Deduplication.DeduplicateNumericKeysInternal(keys, values, out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("CharSorted"), Benchmark(Baseline = true)]
    public int GenericCharSorted()
    {
        char[] keys = Copy(_sortedCharKeys);
        return GenericDeduplicateWithSort(keys, EqualityComparer<char>.Default, Comparer<char>.Default);
    }

    [BenchmarkCategory("CharSorted"), Benchmark]
    public int FastDataCharSorted()
    {
        char[] keys = Copy(_sortedCharKeys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("Int16Sorted"), Benchmark(Baseline = true)]
    public int GenericInt16Sorted()
    {
        short[] keys = Copy(_sortedInt16Keys);
        return GenericDeduplicateWithSort(keys, EqualityComparer<short>.Default, Comparer<short>.Default);
    }

    [BenchmarkCategory("Int16Sorted"), Benchmark]
    public int FastDataInt16Sorted()
    {
        short[] keys = Copy(_sortedInt16Keys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("UInt16Sorted"), Benchmark(Baseline = true)]
    public int GenericUInt16Sorted()
    {
        ushort[] keys = Copy(_sortedUInt16Keys);
        return GenericDeduplicateWithSort(keys, EqualityComparer<ushort>.Default, Comparer<ushort>.Default);
    }

    [BenchmarkCategory("UInt16Sorted"), Benchmark]
    public int FastDataUInt16Sorted()
    {
        ushort[] keys = Copy(_sortedUInt16Keys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("UInt32Sorted"), Benchmark(Baseline = true)]
    public int GenericUInt32Sorted()
    {
        uint[] keys = Copy(_sortedUInt32Keys);
        return GenericDeduplicateWithSort(keys, EqualityComparer<uint>.Default, Comparer<uint>.Default);
    }

    [BenchmarkCategory("UInt32Sorted"), Benchmark]
    public int FastDataUInt32Sorted()
    {
        uint[] keys = Copy(_sortedUInt32Keys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("Int64Sorted"), Benchmark(Baseline = true)]
    public int GenericInt64Sorted()
    {
        long[] keys = Copy(_sortedInt64Keys);
        return GenericDeduplicateWithSort(keys, EqualityComparer<long>.Default, Comparer<long>.Default);
    }

    [BenchmarkCategory("Int64Sorted"), Benchmark]
    public int FastDataInt64Sorted()
    {
        long[] keys = Copy(_sortedInt64Keys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    [BenchmarkCategory("UInt64Sorted"), Benchmark(Baseline = true)]
    public int GenericUInt64Sorted()
    {
        ulong[] keys = Copy(_sortedUInt64Keys);
        return GenericDeduplicateWithSort(keys, EqualityComparer<ulong>.Default, Comparer<ulong>.Default);
    }

    [BenchmarkCategory("UInt64Sorted"), Benchmark]
    public int FastDataUInt64Sorted()
    {
        ulong[] keys = Copy(_sortedUInt64Keys);
        Deduplication.DeduplicateNumericKeysInternal(keys, Array.Empty<int>(), out int uniqueCount);
        return uniqueCount;
    }

    private static int GenericDeduplicateWithSort<TKey>(TKey[] keys, IEqualityComparer<TKey> equalityComparer, IComparer<TKey> sortComparer)
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

    private static int GenericDeduplicateWithSort<TKey, TValue>(TKey[] keys, TValue[] values, IEqualityComparer<TKey> equalityComparer, IComparer<TKey> sortComparer)
    {
        Array.Sort(keys, values, sortComparer);

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

            if (uniqueCount != i)
                values[uniqueCount] = values[i];

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