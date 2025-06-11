using System.Globalization;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Structures;
using Genbox.FastData.InternalShared.TestClasses;

namespace Genbox.FastData.InternalShared.Helpers;

public static class TestVectorHelper
{
    public static IEnumerable<ITestVector> GetTestVectors()
    {
        foreach (ITestVector testVector in GenerateTestVectors(GetSingleValues(), typeof(SingleValueStructure<>)))
        {
            yield return testVector;
        }

        foreach (ITestVector testVector in GenerateTestVectors(GetEdgeCaseValues(),
                     typeof(ArrayStructure<>),
                     typeof(BinarySearchStructure<>),
                     typeof(ConditionalStructure<>),
                     typeof(EytzingerSearchStructure<>),
                     typeof(HashSetChainStructure<>),
                     typeof(HashSetLinearStructure<>)))
        {
            yield return testVector;
        }

        foreach (ITestVector testVector in GenerateTestVectors(GetDataOfSize(100),
                     typeof(ArrayStructure<>),
                     typeof(BinarySearchStructure<>),
                     typeof(ConditionalStructure<>),
                     typeof(EytzingerSearchStructure<>),
                     typeof(HashSetChainStructure<>),
                     typeof(HashSetLinearStructure<>)))
        {
            yield return testVector;
        }

        // We don't include a length of 1, 2 and 4 to check if uniq length structures emit null buckets correctly
        foreach (ITestVector testVector in GenerateTestVectors([["aaa", "aaaaa", "aaaaaa", "aaaaaaa", "aaaaaaaa", "aaaaaaaaa", "aaaaaaaaaa"]], typeof(KeyLengthStructure<>)))
        {
            yield return testVector;
        }

        foreach (ITestVector testVector in GenerateTestVectors([[1, 2, 3]], typeof(HashSetPerfectStructure<>)))
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
                case StructureType.HashSet:
                    yield return new TestData<int>(type, Enumerable.Range(0, benchmarkSize).Select(x => x).ToArray());
                    yield return new TestData<float>(type, Enumerable.Range(0, benchmarkSize).Select(x => (float)x).ToArray());
                    yield return new TestData<string>(type, Enumerable.Range(0, benchmarkSize).Select(x => x.ToString(NumberFormatInfo.InvariantInfo)).ToArray());
                    break;
                default:
                    throw new NotSupportedException("There are no benchmark vectors for " + type);
            }
        }
    }

    private static IEnumerable<ITestVector> GenerateTestVectors(IEnumerable<(Type, object[])> data, params Type[] dataStructs)
    {
        foreach ((Type vt, object[] values) in data)
        {
            foreach (Type st in dataStructs)
            {
                //Convert object[] to T[]
                Array arr = Array.CreateInstance(vt, values.Length);
                for (int i = 0; i < values.Length; i++)
                {
                    arr.SetValue(values[i], i);
                }

                //Create an instance of TestVector<T> and give it the type of the structure
                Type vector = typeof(TestVector<>).MakeGenericType(vt);
                yield return (ITestVector)Activator.CreateInstance(vector, st, arr)!;
            }
        }
    }

    //This overload is to get some little type safety when only a single type is used
    private static IEnumerable<ITestVector> GenerateTestVectors<T>(IEnumerable<T[]> data, params Type[] dataStructs)
    {
        Type type = typeof(T);
        return GenerateTestVectors(data.Select(x => (type, x.Cast<object>().ToArray())), dataStructs);
    }

    private static IEnumerable<(Type type, object[] value)> GetEdgeCaseValues() =>
    [

        // We want to test edge values
        (typeof(sbyte), [sbyte.MinValue, (sbyte)-1, (sbyte)0, (sbyte)1, sbyte.MaxValue]),
        (typeof(byte), [(byte)0, (byte)1, byte.MaxValue]),

        //We keep it within ASCII range as C#'s char does not translate to other languages
        (typeof(char), ['\0', 'a', (char)127]),
        (typeof(double), [double.MinValue, 0.0, 1.0, double.MaxValue]),
        (typeof(float), [float.MinValue, -1f, 0f, 1f, float.MaxValue]),
        (typeof(short), [short.MinValue, (short)-1, (short)0, (short)1, short.MaxValue]),
        (typeof(ushort), [(ushort)0, (ushort)1, (ushort)2, ushort.MaxValue]),
        (typeof(int), [int.MinValue, -1, 0, 1, int.MaxValue]),
        (typeof(uint), [0U, 1U, 2U, uint.MaxValue]),
        (typeof(long), [long.MinValue, -1L, 0L, 1L, long.MaxValue]),
        (typeof(ulong), [0UL, 1UL, 2UL, ulong.MaxValue]),
        (typeof(string), ["a", "item", new string('a', 255)])
    ];

    private static IEnumerable<(Type type, object[] value)> GetDataOfSize(int size) =>
    [
        (typeof(int), Enumerable.Range(0, size).Select(x => x).Cast<object>().ToArray()),
        (typeof(float), Enumerable.Range(0, size).Select(x => (float)x).Cast<object>().ToArray()),
        (typeof(string), Enumerable.Range(0, size).Select(x => x.ToString(NumberFormatInfo.InvariantInfo)).Cast<object>().ToArray())
    ];

    private static IEnumerable<(Type type, object[] value)> GetSingleValues() =>
    [
        (typeof(sbyte), [(sbyte)1]),
        (typeof(byte), [(byte)1]),
        (typeof(char), ['a']),
        (typeof(double), [1.0]),
        (typeof(float), [1f]),
        (typeof(short), [(short)1]),
        (typeof(ushort), [(ushort)1]),
        (typeof(int), [1]),
        (typeof(uint), [1U]),
        (typeof(long), [1L]),
        (typeof(ulong), [1UL]),
        (typeof(string), ["value]"])
    ];
}