using System.Globalization;
using Genbox.FastData.Abstracts;
using Genbox.FastData.ArrayHash;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Analysis.Properties;
using Genbox.FastData.Internal.Structures;
using Genbox.FastData.Misc;

namespace Genbox.FastData.InternalShared;

public static class TestVectorHelper
{
    /// <summary>This variant of TryGenerate calls the public API of FastDataGenerator such that we test it like a user would invoke it.</summary>
    public static bool TryGenerate<T>(Func<string, ICodeGenerator> gen, TestData<T> testData, out GeneratorSpec spec)
    {
        if (FastDataGenerator.TryGenerate(testData.Values, new FastDataConfig(testData.StructureType), gen(testData.Identifier), out string? source))
        {
            spec = new GeneratorSpec(testData.Identifier, source!);
            return true;
        }

        spec = default;
        return false;
    }

    /// <summary>This variant of TryGenerate bypasses the public API to test more advanced combinations of parameters</summary>
    public static bool TryGenerate<T>(Func<string, ICodeGenerator> gen, TestVector<T> vector, out GeneratorSpec spec)
    {
        DataProperties<T> props = DataProperties<T>.Create(vector.Values);

        IContext? context = null;
        IStringHash? stringHash = props.DataType == DataType.String ? vector.StringHash ?? new DefaultStringHash() : null;

        HashFunc<T> hashFunc;
        if (stringHash != null)
            hashFunc = (HashFunc<T>)(object)stringHash.GetHashFunction();
        else
            hashFunc = PrimitiveHash.GetHash<T>(props.DataType);

        object? structure;

        if (vector.Type == typeof(SingleValueStructure<>))
            structure = new SingleValueStructure<T>();
        else if (vector.Type == typeof(BinarySearchStructure<>))
            structure = new BinarySearchStructure<T>(props.DataType, StringComparison.Ordinal);
        else if (vector.Type == typeof(ConditionalStructure<>))
            structure = new ConditionalStructure<T>();
        else if (vector.Type == typeof(EytzingerSearchStructure<>))
            structure = new EytzingerSearchStructure<T>(props.DataType, StringComparison.Ordinal);
        else if (vector.Type == typeof(HashSetChainStructure<>))
            structure = new HashSetChainStructure<T>();
        else if (vector.Type == typeof(HashSetPerfectStructure<>))
            structure = new HashSetPerfectStructure<T>();
        else if (vector.Type == typeof(HashSetLinearStructure<>))
            structure = new HashSetLinearStructure<T>();
        else if (vector.Type == typeof(KeyLengthStructure<>))
            structure = new KeyLengthStructure<T>(props.StringProps!);
        else if (vector.Type == typeof(ArrayStructure<>))
            structure = new ArrayStructure<T>();
        else
            throw new InvalidOperationException("Unsupported structure type: " + vector.Type.Name);

        StructureType structureType = structure switch
        {
            ArrayStructure<T> => StructureType.Array,
            BinarySearchStructure<T> or EytzingerSearchStructure<T> => StructureType.BinarySearch,
            ConditionalStructure<T> => StructureType.Conditional,
            HashSetChainStructure<T> or HashSetPerfectStructure<T> or HashSetLinearStructure<T> => StructureType.HashSet,
            _ => StructureType.Auto
        };

        if (structure is IStructure<T> s)
        {
            if (!s.TryCreate(vector.Values, out context))
            {
                spec = default;
                return false;
            }
        }
        else if (structure is IHashStructure<T> hs)
        {
            if (!hs.TryCreate(vector.Values, hashFunc, out context))
            {
                spec = default;
                return false;
            }
        }

        ICodeGenerator generator = gen(vector.Identifier);
        GeneratorConfig<T> genCfg = new GeneratorConfig<T>(structureType, StringComparison.Ordinal, props, stringHash);
        if (generator.TryGenerate(genCfg, context, out string? source))
        {
            spec = new GeneratorSpec(vector.Identifier, source);
            return true;
        }

        spec = default;
        return false;
    }

    public static IEnumerable<ITestData> GetTestData()
    {
        foreach (StructureType type in Enum.GetValues<StructureType>())
        {
            yield return new TestData<string>(type, ["item1", "item2", "item3"]);
            yield return new TestData<int>(type, [int.MinValue, 0, int.MaxValue]);
            yield return new TestData<long>(type, [long.MinValue, 0, long.MaxValue]);
            yield return new TestData<double>(type, [double.MinValue, 0, double.MaxValue]);
        }
    }

    public static IEnumerable<ITestVector> GetTestVectors()
    {
        foreach (ITestVector testVector in GenerateTestVectors(GetSingleValues(), typeof(SingleValueStructure<>)))
            yield return testVector;

        foreach (ITestVector testVector in GenerateTestVectors(GetEdgeCaseValues(),
                     typeof(ArrayStructure<>),
                     typeof(BinarySearchStructure<>),
                     typeof(ConditionalStructure<>),
                     typeof(EytzingerSearchStructure<>),
                     typeof(HashSetChainStructure<>),
                     typeof(HashSetLinearStructure<>)))
            yield return testVector;

        foreach (ITestVector testVector in GenerateTestVectors(GetDataOfSize(100),
                     typeof(ArrayStructure<>),
                     typeof(BinarySearchStructure<>),
                     typeof(ConditionalStructure<>),
                     typeof(EytzingerSearchStructure<>),
                     typeof(HashSetChainStructure<>),
                     typeof(HashSetLinearStructure<>)))
            yield return testVector;

        // We don't include a length of 1, 2 and 4 to check if uniq length structures emit null buckets correctly
        foreach (ITestVector testVector in GenerateTestVectors([(typeof(string), ["aaa", "aaaaa", "aaaaaa", "aaaaaaa", "aaaaaaaa", "aaaaaaaaa", "aaaaaaaaaa"]),], typeof(KeyLengthStructure<>)))
            yield return testVector;

        // typeof(HashSetPerfectStructure<>),
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
                    arr.SetValue(values[i], i);

                //Create an instance of TestVector<T> and give it the type of the structure
                Type vector = typeof(TestVector<>).MakeGenericType(vt);
                yield return (ITestVector)Activator.CreateInstance(vector, st, arr, null)!;
            }
        }
    }

    private static IEnumerable<(Type type, object[] value)> GetEdgeCaseValues() =>
    [

        // We want to test edge values
        (typeof(bool), [true, false]),
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

        //We use -5 and -1 to avoid hash collisions
        (typeof(long), [long.MinValue, -5L, 0L, 1L, long.MaxValue - 1]),

        //We use -1 to avoid hash collisions
        (typeof(ulong), [0UL, 1UL, 2UL, ulong.MaxValue - 10]),
        (typeof(string), ["a", "item", new string('a', 255)])
    ];

    private static IEnumerable<(Type type, object[] value)> GetDataOfSize(int size) =>
    [
        (typeof(int), Enumerable.Range(0, size).Select(x => x).Cast<object>().ToArray()),
        (typeof(float), Enumerable.Range(0, size).Select(x => (float)x).Cast<object>().ToArray()),
        (typeof(string), Enumerable.Range(0, size).Select(x => x.ToString(NumberFormatInfo.InvariantInfo)).Cast<object>().ToArray()),
    ];

    private static IEnumerable<(Type type, object[] value)> GetSingleValues() =>
    [
        (typeof(bool), [true]),
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