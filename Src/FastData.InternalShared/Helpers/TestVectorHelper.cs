using System.Globalization;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Structures;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.InternalShared.Helpers;

public static class TestVectorHelper
{
    public static IEnumerable<ITestVector> GetFloatNaNZeroTestVectors()
    {
        foreach (ITestVector testVector in GenerateTestVectors(GetFloatSpecialCases(), null, typeof(HashTableStructure<,>)))
            yield return testVector;
    }

    public static IEnumerable<ITestVector> GetSimpleTestVectors()
    {
        foreach (ITestVector testVector in GenerateTestVectors([[1]], typeof(SingleValueStructure<,>)))
            yield return testVector;

        foreach (ITestVector testVector in GenerateTestVectors([["a", "aa", "aaa"]], typeof(KeyLengthStructure<,>)))
            yield return testVector;

        foreach (ITestVector testVector in GenerateTestVectors([[1, 2, 3]],
                     typeof(ArrayStructure<,>),
                     typeof(BinarySearchStructure<,>),
                     typeof(ConditionalStructure<,>),
                     typeof(HashTableStructure<,>),
                     typeof(HashTablePerfectStructure<,>)))
        {
            yield return testVector;
        }
    }

    public static IEnumerable<ITestVector> GetTestVectors()
    {
        foreach (ITestVector testVector in GenerateTestVectors(GetSingleValues(), null, typeof(SingleValueStructure<,>)))
        {
            yield return testVector;
        }

        foreach (ITestVector testVector in GenerateTestVectors(GetEdgeCaseValues(), null,
                     typeof(ArrayStructure<,>),
                     typeof(BinarySearchStructure<,>),
                     typeof(ConditionalStructure<,>),
                     typeof(HashTableStructure<,>)))
        {
            yield return testVector;
        }

        foreach (ITestVector testVector in GenerateTestVectors(GetDataOfSize(100), null,
                     typeof(ArrayStructure<,>),
                     typeof(BinarySearchStructure<,>),
                     typeof(ConditionalStructure<,>),
                     typeof(HashTableStructure<,>)))
        {
            yield return testVector;
        }

        // We don't include a length of 1, 2 and 4 to check if uniq length structures emit null buckets correctly
        foreach (ITestVector testVector in GenerateTestVectors([["aaa", "aaaaa", "aaaaaa", "aaaaaaa", "aaaaaaaa", "aaaaaaaaa", "aaaaaaaaaa"]], typeof(KeyLengthStructure<,>)))
        {
            yield return testVector;
        }

        foreach (ITestVector testVector in GenerateTestVectors([[1, 2, 3]], typeof(HashTablePerfectStructure<,>)))
        {
            yield return testVector;
        }

        // Strings with characters that are not in the ASCII range
        foreach (ITestVector testVector in GenerateTestVectors([["æ", "à", "ä", "ö", "ü", "ß", "é", "è", "ê", "ç", "ñ", "ø", "å"]],
                     typeof(ArrayStructure<,>),
                     typeof(BinarySearchStructure<,>),
                     typeof(ConditionalStructure<,>),
                     typeof(HashTableStructure<,>)))
        {
            yield return testVector;
        }
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

    private static IEnumerable<ITestVector> GenerateTestVectors(IEnumerable<(Type, object[], object[])> data, string? postfix = null, params Type[] dataStructs)
    {
        foreach ((Type vt, object[] values, object[] notValues) in data)
        {
            foreach (Type st in dataStructs)
            {
                //Convert object[] to T[]
                Array arr = Array.CreateInstance(vt, values.Length);
                for (int i = 0; i < values.Length; i++)
                {
                    arr.SetValue(values[i], i);
                }

                Array arr2 = Array.CreateInstance(vt, notValues.Length);
                for (int i = 0; i < notValues.Length; i++)
                {
                    arr2.SetValue(notValues[i], i);
                }

                //Create an instance of TestVector<T> and give it the type of the structure
                Type vector = typeof(TestVector<>).MakeGenericType(vt);
                yield return (ITestVector)Activator.CreateInstance(vector, st, arr, arr2, postfix)!;
            }
        }
    }

    //This overload is to get some little type safety when only a single type is used
    private static IEnumerable<ITestVector> GenerateTestVectors<T>(IEnumerable<T[]> data, params Type[] dataStructs)
    {
        Type type = typeof(T);
        return GenerateTestVectors(data.Select(x => (type, x.Cast<object>().ToArray(), Array.Empty<object>())), null, dataStructs);
    }

    private static IEnumerable<(Type type, object[] value, object[] notValue)> GetEdgeCaseValues() =>
    [

        // We want to test edge values
        (typeof(sbyte), [sbyte.MinValue, (sbyte)-1, (sbyte)0, (sbyte)1, sbyte.MaxValue], [(sbyte)-2, (sbyte)2]),
        (typeof(byte), [(byte)0, (byte)1, byte.MaxValue], [(byte)2, (byte)3]),

        //We keep it within ASCII range as C#'s char does not translate to other languages
        (typeof(char), ['\0', 'a', (char)127], [(char)1, 'b']),
        (typeof(double), [double.MinValue, 0.0, 1.0, double.MaxValue], [1.1, 2.0]),
        (typeof(float), [float.MinValue, -1f, 0f, 1f, float.MaxValue], [1.1f, 2.0f]),
        (typeof(short), [short.MinValue, (short)-1, (short)0, (short)1, short.MaxValue], [(short)-2, (short)2]),
        (typeof(ushort), [(ushort)0, (ushort)1, (ushort)2, ushort.MaxValue], [(ushort)3, (ushort)4]),
        (typeof(int), [int.MinValue, -1, 0, 1, int.MaxValue], [-2, 2]),
        (typeof(uint), [0U, 1U, 2U, uint.MaxValue], [3U, 4U]),
        (typeof(long), [long.MinValue, -1L, 0L, 1L, long.MaxValue], [-2L, 2L]),
        (typeof(ulong), [0UL, 1UL, 2UL, ulong.MaxValue], [3UL, 4UL]),
        (typeof(string), ["a", "item", new string('a', 255)], ["b", "item2"])
    ];

    private static IEnumerable<(Type type, object[] value, object[] notValue)> GetFloatSpecialCases() =>
    [

        //If we don't have zero or NaN, we can use a simple hash
        (typeof(float), [1, 2, 3, 4, 5], []),
        (typeof(double), [1, 2, 3, 4, 5], []),
    ];

    private static IEnumerable<(Type type, object[] value, object[] notValue)> GetDataOfSize(int size) =>
    [
        (typeof(int), Enumerable.Range(0, size).Select(x => x).Cast<object>().ToArray(), Enumerable.Range(size, size * 2).Select(x => x).Cast<object>().ToArray()),
        (typeof(float), Enumerable.Range(0, size).Select(x => (float)x).Cast<object>().ToArray(), Enumerable.Range(size, size * 2).Select(x => (float)x).Cast<object>().ToArray()),
        (typeof(string), Enumerable.Range(0, size).Select(x => x.ToString(NumberFormatInfo.InvariantInfo)).Cast<object>().ToArray(), Enumerable.Range(size, size * 2).Select(x => x.ToString(NumberFormatInfo.InvariantInfo)).Cast<object>().ToArray())
    ];

    private static IEnumerable<(Type type, object[] value, object[] notValue)> GetSingleValues() =>
    [
        (typeof(sbyte), [(sbyte)1], [(sbyte)2]),
        (typeof(byte), [(byte)1], [(byte)2]),
        (typeof(char), ['a'], ['b']),
        (typeof(double), [1.0], [2.0]),
        (typeof(float), [1f], [2f]),
        (typeof(short), [(short)1], [(short)2]),
        (typeof(ushort), [(ushort)1], [(ushort)2]),
        (typeof(int), [1], [2]),
        (typeof(uint), [1U], [2U]),
        (typeof(long), [1L], [2L]),
        (typeof(ulong), [1UL], [2UL]),
        (typeof(string), ["value"], ["eulav"])
    ];
}