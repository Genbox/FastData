using System.Globalization;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Structures;
using Genbox.FastData.InternalShared.Code;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.InternalShared.Helpers;

public static class TestVectorHelper
{
    public static IEnumerable<ITestVector> GetFloatNaNZeroTestVectors()
    {
        foreach (ITestVector testVector in GenerateTestVectors(GetFloatSpecialCases(), null, typeof(HashTableStructure<,>)))
            yield return testVector;
    }

    public static IEnumerable<ITestVector> GetKeyValueTestVectors()
    {
        // First we try with a simple value
        int[] simpleValues = [1, 2, 3];

        foreach (ITestVector testVector in GenerateTestVectors([[1]], [[simpleValues[0]]], "simple", typeof(SingleValueStructure<,>)))
            yield return testVector;

        foreach (ITestVector testVector in GenerateTestVectors([["a", "aa", "aaa"]], [simpleValues], "simple", typeof(KeyLengthStructure<,>)))
            yield return testVector;

        foreach (ITestVector testVector in GenerateTestVectors([[1, 2, 3]], [simpleValues], "simple",
                     typeof(ArrayStructure<,>),
                     typeof(BinarySearchStructure<,>),
                     typeof(ConditionalStructure<,>),
                     typeof(HashTableStructure<,>),
                     typeof(HashTablePerfectStructure<,>)))
        {
            yield return testVector;
        }

        // Then we try with complex values
        Person[] complexValues =
        [
            new Person { Age = 1, Name = "Bob", Other = new Person { Name = "Anna", Age = 4 } },
            new Person { Age = 2, Name = "Billy" },
            new Person { Age = 3, Name = "Bibi" },
        ];

        foreach (ITestVector testVector in GenerateTestVectors([[1]], [[complexValues[0]]], "complex", typeof(SingleValueStructure<,>)))
            yield return testVector;

        foreach (ITestVector testVector in GenerateTestVectors([["a", "aa", "aaa"]], [complexValues], "complex", typeof(KeyLengthStructure<,>)))
            yield return testVector;

        foreach (ITestVector testVector in GenerateTestVectors([[1, 2, 3]], [complexValues], "complex",
                     typeof(ArrayStructure<,>),
                     typeof(BinarySearchStructure<,>),
                     typeof(BitSetStructure<,>),
                     typeof(ConditionalStructure<,>),
                     typeof(HashTableStructure<,>),
                     typeof(HashTablePerfectStructure<,>)))
        {
            yield return testVector;
        }
    }

    public static IEnumerable<ITestVector> GetValueTestVectors()
    {
        foreach (ITestVector testVector in GenerateTestVectors(GetSingleValues(), null, typeof(SingleValueStructure<,>)))
            yield return testVector;

        foreach (ITestVector testVector in GenerateTestVectors(GetEdgeCaseValues(), null,
                     typeof(ArrayStructure<,>),
                     typeof(BinarySearchStructure<,>),
                     typeof(ConditionalStructure<,>),
                     typeof(HashTableStructure<,>),
                     typeof(HashTableCompactStructure<,>)))
        {
            yield return testVector;
        }

        foreach (ITestVector testVector in GenerateTestVectors(GetDataOfSize(100), null,
                     typeof(ArrayStructure<,>),
                     typeof(BloomFilterStructure<,>),
                     typeof(BinarySearchStructure<,>),
                     typeof(ConditionalStructure<,>),
                     typeof(HashTableStructure<,>),
                     typeof(HashTableCompactStructure<,>)))
        {
            yield return testVector;
        }

        foreach (ITestVector testVector in GenerateTestVectors(GetNaturallySparseIntData(1000), "natural_sparse", typeof(EliasFanoStructure<,>), typeof(RrrBitVectorStructure<,>)))
            yield return testVector;

        foreach (ITestVector testVector in GenerateTestVectors(GetNaturallySparseNegativeIntData(1000), "natural_sparse_negative", typeof(EliasFanoStructure<,>), typeof(RrrBitVectorStructure<,>)))
            yield return testVector;

        // We don't include a length of 1, 2 and 4 to check if uniq length structures emit null buckets correctly
        foreach (ITestVector testVector in GenerateTestVectors([["aaa", "aaaaa", "aaaaaa", "aaaaaaa", "aaaaaaaa", "aaaaaaaaa", "aaaaaaaaaa"]], null, typeof(KeyLengthStructure<,>)))
            yield return testVector;

        foreach (ITestVector testVector in GenerateTestVectors([[1, 2, 3]], null, typeof(HashTablePerfectStructure<,>)))
            yield return testVector;

        // Strings with characters that are not in the ASCII range
        foreach (ITestVector testVector in GenerateTestVectors([["æ", "à", "ä", "ö", "ü", "ß", "é", "è", "ê", "ç", "ñ", "ø", "å"]], "non_ascii",
                     typeof(ArrayStructure<,>),
                     typeof(BloomFilterStructure<,>),
                     typeof(BinarySearchStructure<,>),
                     typeof(ConditionalStructure<,>),
                     typeof(HashTableCompactStructure<,>),
                     typeof(HashTableStructure<,>)))
        {
            yield return testVector;
        }

        // Test range/bitset support. Keys have to be without gaps for range to kick in.
        foreach (ITestVector testVector in GenerateTestVectors([[1, 2, 3, 4, 5]], "range_bitset", typeof(RangeStructure<,>), typeof(BitSetStructure<,>)))
            yield return testVector;

        // Test prefix/suffix support
        foreach (ITestVector testVector in GenerateTestVectors([["pretext", "prefetch", "prefix"], ["suffix", "prefix"]], "prefix_suffix", typeof(ArrayStructure<,>)))
            yield return testVector;
    }

    public static IEnumerable<ITestData> GetBenchmarkData()
    {
        const int benchmarkSize = 1000;

        foreach (StructureType type in Enum.GetValues<StructureType>())
        {
            switch (type)
            {
                //We skip these
                case StructureType.Auto:
                    continue;

                case StructureType.Conditional:
                case StructureType.BinarySearch:
                case StructureType.Array:
                case StructureType.HashTable:
                    yield return new TestData<int>(type, Enumerable.Range(0, benchmarkSize).Select(x => x).ToArray());
                    yield return new TestData<float>(type, Enumerable.Range(0, benchmarkSize).Select(x => (float)x).ToArray());
                    yield return new TestData<string>(type, Enumerable.Range(0, benchmarkSize).Select(x => x.ToString(NumberFormatInfo.InvariantInfo)).ToArray());
                    break;
                default:
                    throw new NotSupportedException("There are no benchmark vectors for " + type);
            }
        }
    }

    private static IEnumerable<ITestVector> GenerateTestVectors(IEnumerable<DataPair> pairs, string? postfix = null, params Type[] dataStructs)
    {
        foreach ((object[] keys, object[] notInKeys, object[]? values) in pairs)
        {
            Type keyType = keys[0].GetType();

            foreach (Type st in dataStructs)
            {
                //Convert object[] to T[]
                Array keysArr = Array.CreateInstance(keyType, keys.Length);
                for (int i = 0; i < keys.Length; i++)
                {
                    keysArr.SetValue(keys[i], i);
                }

                Array notInKeysArr = Array.CreateInstance(keyType, notInKeys.Length);
                for (int i = 0; i < notInKeys.Length; i++)
                {
                    notInKeysArr.SetValue(notInKeys[i], i);
                }

                if (values != null)
                {
                    Type valueType = values[0].GetType();

                    Array valuesArr = Array.CreateInstance(valueType, values.Length);
                    for (int i = 0; i < values.Length; i++)
                    {
                        valuesArr.SetValue(values[i], i);
                    }

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

    private static IEnumerable<ITestVector> GenerateTestVectors<TKey>(IEnumerable<TKey[]> keySets, string? postfix = null, params Type[] dataStructs)
    {
        return GenerateTestVectors(keySets.Select(x => new DataPair(x.Cast<object>().ToArray(), [])), postfix, dataStructs);
    }

    private static IEnumerable<ITestVector> GenerateTestVectors<TKey, TValue>(TKey[][] keySets, TValue[][] valueSets, string? postFix = null, params Type[] dataStructs)
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

    private static DataPair[] GetFloatSpecialCases() =>
    [
        //If we don't have zero or NaN, we can use a simple hash
        new DataPair([1f, 2f, 3f, 4f, 5f], []),
        new DataPair([1.0, 2.0, 3.0, 4.0, 5.0], []),
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
