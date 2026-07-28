using System.Globalization;
using Genbox.FastData.Enums;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Abstracts;
using Genbox.FastData.Generators.EarlyExits.Exits;
using Genbox.FastData.InternalShared.Misc;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.InternalShared.Helpers;

public static class TestVectorHelper
{
    public static IEnumerable<ITestVector> GetKeyValueTestVectors()
    {
        // First we try with a simple value
        int[] simpleValues = [1, 2, 3];

        foreach (ITestVector testVector in GenerateTestVectors([[1]], [[simpleValues[0]]], "simple", StructureType.SingleValue))
            yield return testVector;

        foreach (ITestVector testVector in GenerateTestVectors([["a", "aa", "aaa"]], [simpleValues], "simple", StructureType.KeyLength))
            yield return testVector;

        foreach (ITestVector testVector in GenerateTestVectors([[1, 2, 3]], [simpleValues], "simple",
                     StructureType.Array,
                     StructureType.BinarySearch,
                     StructureType.BinarySearchInterpolation,
                     StructureType.Conditional,
                     StructureType.ConstMap,
                     StructureType.HashTable,
                     StructureType.HashTablePerfect,
                     StructureType.Hyble,
                     StructureType.Pgm))
            yield return testVector;

        // Then we try with complex values
        Person[] complexValues =
        [
            new Person { Age = 1, Name = "Bob", Other = new Person { Name = "Anna", Age = 4 } },
            new Person { Age = 2, Name = "Billy" },
            new Person { Age = 3, Name = "Bibi" }
        ];

        foreach (ITestVector testVector in GenerateTestVectors([[1]], [[complexValues[0]]], "complex", StructureType.SingleValue))
            yield return testVector;

        foreach (ITestVector testVector in GenerateTestVectors([["a", "aa", "aaa"]], [complexValues], "complex", StructureType.KeyLength))
            yield return testVector;

        foreach (ITestVector testVector in GenerateTestVectors([[1, 2, 3]], [complexValues], "complex",
                     StructureType.Array,
                     StructureType.BinarySearch,
                     StructureType.BinarySearchInterpolation,
                     StructureType.BitSet,
                     StructureType.Conditional,
                     StructureType.HashTable,
                     StructureType.HashTablePerfect,
                     StructureType.Hyble,
                     StructureType.Pgm))
            yield return testVector;
    }

    public static IEnumerable<ITestVector> GetValueTestVectors()
    {
        foreach (ITestVector testVector in GenerateTestVectors(GetSingleValues(), null, StructureType.SingleValue))
            yield return testVector;

        foreach (ITestVector testVector in GenerateTestVectors(GetEdgeCaseValues(), null,
                     StructureType.Array,
                     StructureType.BinarySearch,
                     StructureType.Conditional,
                     StructureType.HashTable,
                     StructureType.HashTableCompact))
            yield return testVector;

        foreach (ITestVector testVector in GenerateTestVectors(GetDataOfSize(100), null,
                     StructureType.Array,
                     StructureType.BloomFilter,
                     StructureType.BinarySearch,
                     StructureType.Conditional,
                     StructureType.HashTable,
                     StructureType.HashTableCompact))
            yield return testVector;

        foreach (ITestVector testVector in GenerateTestVectors(GetNaturallySparseIntData(1000), "natural_sparse", StructureType.EliasFano, StructureType.RrrBitVector))
            yield return testVector;

        foreach (ITestVector testVector in GenerateTestVectors(GetNaturallySparseNegativeIntData(1000), "natural_sparse_negative", StructureType.EliasFano, StructureType.RrrBitVector))
            yield return testVector;

        // We don't include a length of 1, 2 and 4 to check if uniq length structures emit null buckets correctly
        foreach (ITestVector testVector in GenerateTestVectors([["aaa", "aaaaa", "aaaaaa", "aaaaaaa", "aaaaaaaa", "aaaaaaaaa", "aaaaaaaaaa"]], null, StructureType.KeyLength))
            yield return testVector;

        foreach (ITestVector testVector in GenerateTestVectors([[1, 2, 3]], null, StructureType.ConstMap, StructureType.HashTablePerfect, StructureType.Hyble))
            yield return testVector;

        // Strings with characters that are not in the ASCII range
        foreach (ITestVector testVector in GenerateTestVectors([["æ", "à", "ä", "ö", "ü", "ß", "é", "è", "ê", "ç", "ñ", "ø", "å"]], "non_ascii",
                     StructureType.Array,
                     StructureType.BloomFilter,
                     StructureType.BinarySearch,
                     StructureType.Conditional,
                     StructureType.ConstMap,
                     StructureType.HashTableCompact,
                     StructureType.HashTable))
            yield return testVector;

        foreach (ITestVector testVector in GenerateTestVectors([[1, 2, 3, 4, 5]], "sorted_numeric", StructureType.BinarySearchInterpolation, StructureType.BitSet, StructureType.Pgm, StructureType.Range))
            yield return testVector;

        foreach (ITestVector testVector in GenerateTestVectors([[1f, 2f, 3f, 4f, 5f]], "sorted_numeric", StructureType.BinarySearchInterpolation, StructureType.Pgm))
            yield return testVector;

        // Larger sorted, non-uniform numeric dataset for structures that depend on value distribution.
        foreach (ITestVector testVector in GenerateTestVectors(GetNonUniformSortedIntData(200), "non_uniform_sorted", StructureType.BinarySearchInterpolation, StructureType.Pgm, StructureType.Range))
            yield return testVector;
    }

    public static IEnumerable<ITestData> GetBenchmarkData(int warmupSampleCount = 5, int minSampleCount = 10, int maxSampleCount = 10, int targetIterationTimeMs = 100, int benchmarkSize = 1000, int keyLengthBenchmarkSize = 128, BenchmarkWorkload workload = BenchmarkWorkload.Mixed, double maxErrorPercent = 2.0d)
    {
        int[] intKeys = Enumerable.Range(0, benchmarkSize).ToArray();
        float[] floatKeys = Enumerable.Range(0, benchmarkSize).Select(x => (float)x).ToArray();
        float[] perfectFloatHashKeys = CreatePerfectFloatHashKeys(benchmarkSize);

        StructureType[] generalTypes =
        [
            StructureType.Array,
            StructureType.BinarySearch,
            StructureType.BloomFilter,
            StructureType.Conditional,
            StructureType.ConstMap,
            StructureType.HashTable,
            StructureType.HashTableCompact
        ];

        foreach (StructureType type in generalTypes)
        {
            yield return CreateTestData(type, intKeys);
            yield return CreateTestData(type, floatKeys);
        }

        StructureType[] numericTypes = [StructureType.BinarySearchInterpolation, StructureType.Pgm];

        foreach (StructureType type in numericTypes)
        {
            yield return CreateTestData(type, intKeys);
            yield return CreateTestData(type, floatKeys);
        }

        StructureType[] integralTypes = [StructureType.BitSet, StructureType.EliasFano, StructureType.Range, StructureType.RrrBitVector];

        foreach (StructureType type in integralTypes)
            yield return CreateTestData(type, intKeys);

        yield return CreateTestData(StructureType.HashTablePerfect, intKeys);
        yield return CreateTestData(StructureType.HashTablePerfect, perfectFloatHashKeys);

        yield return CreateTestData(StructureType.Hyble, intKeys);

        string[] stringKeys = Enumerable.Range(0, benchmarkSize).Select(static x => $"item-{x}").ToArray();
        yield return CreateTestData(StructureType.ConstMap, stringKeys);

        string[] uniqueLengthStringKeys = Enumerable.Range(1, keyLengthBenchmarkSize).Select(x => new string('a', x)).ToArray();
        yield return CreateTestData(StructureType.KeyLength, uniqueLengthStringKeys);

        ITestData CreateTestData<TKey>(StructureType type, TKey[] keys) => new TestData<TKey>(type, keys, workload, warmupSampleCount, minSampleCount, maxSampleCount, targetIterationTimeMs, maxErrorPercent);
    }

    public static IEnumerable<ITestData> GetEarlyExitBenchmarkData(int warmupSampleCount = 5, int minSampleCount = 10, int maxSampleCount = 10, int targetIterationTimeMs = 100, int benchmarkSize = 1000, BenchmarkWorkload workload = BenchmarkWorkload.Mixed, double maxErrorPercent = 2.0d)
    {
        // --- Individual numeric exits (int keys) ---

        int[] intHitKeys = Enumerable.Range(500, benchmarkSize).ToArray();

        yield return CreateEarlyExitData([new ValueLessThanEarlyExit<int>(500)],
            intHitKeys, Enumerable.Range(0, benchmarkSize).Select(x => x % 500).ToArray(),
            GeneratorFunction.None, "ValueLessThan");

        yield return CreateEarlyExitData([new ValueGreaterThanEarlyExit<int>(500)],
            Enumerable.Range(0, benchmarkSize).Select(x => x % 501).ToArray(),
            Enumerable.Range(501, benchmarkSize).ToArray(),
            GeneratorFunction.None, "ValueGreaterThan");

        yield return CreateEarlyExitData([new ValueInRangeEarlyExit<int>(400, 600)],
            Enumerable.Range(0, benchmarkSize).Select(x => x % 401).ToArray(),
            Enumerable.Range(0, benchmarkSize).Select(x => 401 + (x % 199)).ToArray(),
            GeneratorFunction.None, "ValueInRange");

        yield return CreateEarlyExitData([new ValueBitMaskEarlyExit(0x00FF00ul)],
            Enumerable.Range(0, benchmarkSize).Select(x => x % 256).ToArray(),
            Enumerable.Range(0, benchmarkSize).Select(x => 256 + x).ToArray(),
            GeneratorFunction.None, "ValueBitMask");

        // --- Individual string exits ---

        int stringSize = Math.Min(benchmarkSize, 200);
        string[] strHitKeys = Enumerable.Range(5, stringSize).Select(x => new string('a', x)).ToArray();
        string[] strMissKeys = Enumerable.Range(1, stringSize).Select(x => new string('b', (x % 4) + 1)).ToArray();

        yield return CreateEarlyExitData([new LengthLessThanEarlyExit(5)],
            strHitKeys, strMissKeys,
            GeneratorFunction.Length, "LengthLessThan");

        yield return CreateEarlyExitData([new LengthGreaterThanEarlyExit(10)],
            Enumerable.Range(1, stringSize).Select(x => new string('a', (x % 10) + 1)).ToArray(),
            Enumerable.Range(1, stringSize).Select(x => new string('b', x + 10)).ToArray(),
            GeneratorFunction.Length, "LengthGreaterThan");

        ulong lengthBitmap = (1ul << 2) | (1ul << 4) | (1ul << 6) | (1ul << 8);
        yield return CreateEarlyExitData([new LengthBitmapEarlyExit(lengthBitmap)],
            Enumerable.Range(0, stringSize).Select(x => new string('a', new[] { 3, 5, 7, 9 }[x % 4])).ToArray(),
            Enumerable.Range(0, stringSize).Select(x => new string('b', new[] { 2, 4, 6, 8 }[x % 4])).ToArray(),
            GeneratorFunction.Length, "LengthBitmap");

        ulong charBitmapHigh = (1ul << ('a' - 64)) | (1ul << ('b' - 64)) | (1ul << ('c' - 64)) | (1ul << ('d' - 64)) | (1ul << ('e' - 64)) | (1ul << ('f' - 64));

        yield return CreateEarlyExitData([new UnitAtBitmapEarlyExit(0ul, charBitmapHigh, false)],
            Enumerable.Range(0, stringSize).Select(x => (char)('a' + (x % 6)) + "test").ToArray(),
            Enumerable.Range(0, stringSize).Select(x => (char)('g' + (x % 20)) + "test").ToArray(),
            GeneratorFunction.UnitAt | GeneratorFunction.Length, "UnitAtBitmap");

        // --- Combined exits ---

        yield return CreateEarlyExitData([new ValueLessThanEarlyExit<int>(100), new ValueGreaterThanEarlyExit<int>(900)],
            Enumerable.Range(100, 801).Take(benchmarkSize).ToArray(),
            Enumerable.Range(0, benchmarkSize).Select(x => x < benchmarkSize / 2 ? x % 100 : 901 + (x % 100)).ToArray(),
            GeneratorFunction.None, "ValueLessThan_ValueGreaterThan");

        yield return CreateEarlyExitData([new LengthLessThanEarlyExit(5), new LengthGreaterThanEarlyExit(15)],
            Enumerable.Range(0, stringSize).Select(x => new string('a', (x % 11) + 5)).ToArray(),
            Enumerable.Range(0, stringSize).Select(x => x % 2 == 0 ? new string('b', (x % 4) + 1) : new string('b', (x % 10) + 16)).ToArray(),
            GeneratorFunction.Length, "LengthLessThan_LengthGreaterThan");

        yield return CreateEarlyExitData([new LengthLessThanEarlyExit(3), new UnitAtBitmapEarlyExit(0ul, charBitmapHigh, false)],
            Enumerable.Range(0, stringSize).Select(x => (char)('a' + (x % 6)) + new string('x', (x % 10) + 2)).ToArray(),
            Enumerable.Range(0, stringSize).Select(x => x % 2 == 0 ? new string('g', 1) : (char)('g' + (x % 20)) + "test").ToArray(),
            GeneratorFunction.UnitAt | GeneratorFunction.Length, "LengthLessThan_UnitAtBitmap");

        ITestData CreateEarlyExitData<TKey>(IEarlyExit[] exits, TKey[] hitKeys, TKey[] missKeys, GeneratorFunction functions, string name) =>
            new EarlyExitTestData<TKey>(exits, hitKeys, missKeys, functions, name, workload, warmupSampleCount, minSampleCount, maxSampleCount, targetIterationTimeMs, maxErrorPercent);
    }

    private static float[] CreatePerfectFloatHashKeys(int size)
    {
        float[] keys = new float[size];

        // Default float hashing uses the raw bit pattern when zero is absent.
        for (int i = 0; i < keys.Length; i++)
            keys[i] = BitConverter.Int32BitsToSingle(i + 1);

        return keys;
    }

    private static IEnumerable<ITestVector> GenerateTestVectors(IEnumerable<DataPair> pairs, string? postfix = null, params StructureType[] dataStructs)
    {
        foreach ((object[] keys, object[] notInKeys, object[]? values) in pairs)
        {
            Type keyType = keys[0].GetType();

            foreach (StructureType st in dataStructs)
            {
                //Convert object[] to T[]
                Array keysArr = Array.CreateInstance(keyType, keys.Length);
                for (int i = 0; i < keys.Length; i++)
                    keysArr.SetValue(keys[i], i);

                Array notInKeysArr = Array.CreateInstance(keyType, notInKeys.Length);
                for (int i = 0; i < notInKeys.Length; i++)
                    notInKeysArr.SetValue(notInKeys[i], i);

                if (values != null)
                {
                    Type valueType = values[0].GetType();

                    Array valuesArr = Array.CreateInstance(valueType, values.Length);
                    for (int i = 0; i < values.Length; i++)
                        valuesArr.SetValue(values[i], i);

                    Type vector = typeof(TestVector<,>).MakeGenericType(keyType, valueType);
                    yield return (ITestVector)Activator.CreateInstance(vector, st, keysArr, notInKeysArr, valuesArr, postfix)!;
                }
                else
                {
                    Type vector = typeof(TestVector<>).MakeGenericType(keyType);
                    yield return (ITestVector)Activator.CreateInstance(vector, st, keysArr, notInKeysArr, postfix)!;
                }
            }
        }
    }

    private static IEnumerable<ITestVector> GenerateTestVectors<TKey>(IEnumerable<TKey[]> keySets, string? postfix = null, params StructureType[] dataStructs)
    {
        return GenerateTestVectors(keySets.Select(x => new DataPair(x.Cast<object>().ToArray(), [])), postfix, dataStructs);
    }

    private static IEnumerable<ITestVector> GenerateTestVectors<TKey, TValue>(TKey[][] keySets, TValue[][] valueSets, string? postFix = null, params StructureType[] dataStructs)
    {
        if (keySets.Length != valueSets.Length)
            throw new InvalidOperationException("The number of key sets does not match the number of value sets.");

        return GenerateTestVectors(CreatePairs(), postFix, dataStructs);

        IEnumerable<DataPair> CreatePairs()
        {
            for (int i = 0; i < keySets.Length; i++)
                yield return new DataPair(keySets[i].Cast<object>().ToArray(), [], valueSets[i].Cast<object>().ToArray());
        }
    }

    private static DataPair[] GetEdgeCaseValues() =>
    [
        // We want to test edge values
        new DataPair([sbyte.MinValue, (sbyte)-1, (sbyte)0, (sbyte)1, sbyte.MaxValue], [(sbyte)-2, (sbyte)2]),
        new DataPair([(byte)0, (byte)1, byte.MaxValue], [(byte)2, (byte)3]),

        //We keep it within ASCII range as C#'s char does not translate to other languages
        new DataPair(['\0', 'a', (char)127], [(char)1, 'b']),
        new DataPair([double.MinValue, 0.0, 1.0, double.MaxValue], [1.1, 2.0]),
        new DataPair([float.MinValue, -1f, 0f, 1f, float.MaxValue], [1.1f, 2.0f]),
        new DataPair([short.MinValue, (short)-1, (short)0, (short)1, short.MaxValue], [(short)-2, (short)2]),
        new DataPair([(ushort)0, (ushort)1, (ushort)2, ushort.MaxValue], [(ushort)3, (ushort)4]),
        new DataPair([int.MinValue, -1, 0, 1, int.MaxValue], [-2, 2]),
        new DataPair([0U, 1U, 2U, uint.MaxValue], [3U, 4U]),
        new DataPair([long.MinValue, -1L, 0L, 1L, long.MaxValue], [-2L, 2L]),
        new DataPair([0UL, 1UL, 2UL, ulong.MaxValue], [3UL, 4UL]),
        new DataPair(["a", "item", new string('a', 255)], ["b", "item2"])
    ];

    private static DataPair[] GetDataOfSize(int size) =>
    [
        new DataPair(Enumerable.Range(0, size).Select(x => x).Cast<object>().ToArray(), Enumerable.Range(size, size * 2).Select(x => x).Cast<object>().ToArray()),
        new DataPair(Enumerable.Range(0, size).Select(x => (float)x).Cast<object>().ToArray(), Enumerable.Range(size, size * 2).Select(x => (float)x).Cast<object>().ToArray()),
        new DataPair(Enumerable.Range(0, size).Select(x => x.ToString(NumberFormatInfo.InvariantInfo)).Cast<object>().ToArray(), Enumerable.Range(size, size * 2).Select(x => x.ToString(NumberFormatInfo.InvariantInfo)).Cast<object>().ToArray())
    ];

    private static DataPair[] GetNaturallySparseIntData(int size)
    {
        int[] keys = new int[size];
        int value = 10_000;

        for (int i = 0; i < size; i++)
        {
            value += 7 + (i % 11);

            if (i % 31 == 0)
                value += 120;

            if (i % 127 == 0)
                value += 1600;

            keys[i] = value;
        }

        int notPresentCount = Math.Min(256, size);
        int[] notPresent = new int[notPresentCount];

        for (int i = 0; i < notPresentCount; i++)
            notPresent[i] = keys[i] - 1;

        return [new DataPair(keys.Cast<object>().ToArray(), notPresent.Cast<object>().ToArray())];
    }

    private static DataPair[] GetNaturallySparseNegativeIntData(int size)
    {
        int[] keys = new int[size];
        int value = -200_000;

        for (int i = 0; i < size; i++)
        {
            value += 7 + (i % 11);

            if (i % 31 == 0)
                value += 120;

            if (i % 127 == 0)
                value += 1600;

            keys[i] = value;
        }

        int notPresentCount = Math.Min(256, size);
        int[] notPresent = new int[notPresentCount];

        for (int i = 0; i < notPresentCount; i++)
            notPresent[i] = keys[i] - 1;

        return [new DataPair(keys.Cast<object>().ToArray(), notPresent.Cast<object>().ToArray())];
    }

    private static DataPair[] GetNonUniformSortedIntData(int size)
    {
        int[] keys = new int[size];
        int value = 100;

        for (int i = 0; i < size; i++)
        {
            value += 3 + (i % 7);

            if (i % 23 == 0)
                value += 50;

            keys[i] = value;
        }

        int notPresentCount = Math.Min(128, size);
        int[] notPresent = new int[notPresentCount];

        for (int i = 0; i < notPresentCount; i++)
            notPresent[i] = keys[i] - 1;

        return [new DataPair(keys.Cast<object>().ToArray(), notPresent.Cast<object>().ToArray())];
    }

    private static DataPair[] GetSingleValues() =>
    [
        new DataPair([(sbyte)1], [(sbyte)2]),
        new DataPair([(byte)1], [(byte)2]),
        new DataPair(['a'], ['b']),
        new DataPair([1.0], [2.0]),
        new DataPair([1f], [2f]),
        new DataPair([(short)1], [(short)2]),
        new DataPair([(ushort)1], [(ushort)2]),
        new DataPair([1], [2]),
        new DataPair([1U], [2U]),
        new DataPair([1L], [2L]),
        new DataPair([1UL], [2UL]),
        new DataPair(["value"], ["eulav"])
    ];

    private record struct DataPair(object[] Keys, object[] NotInKeys, object[]? Values = null);
}