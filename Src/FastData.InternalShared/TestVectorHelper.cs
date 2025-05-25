using System.Globalization;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;

namespace Genbox.FastData.InternalShared;

public static class TestVectorHelper
{
    public static bool TryGenerate<T>(Func<string, ICodeGenerator> gen, TestData<T> data, out GeneratorSpec spec)
    {
        if (FastDataGenerator.TryGenerate(data.Values, new FastDataConfig(data.StructureType), gen(data.Identifier), out string? source))
        {
            spec = new GeneratorSpec(data.Identifier, source!);
            return true;
        }

        spec = default;
        return false;
    }

    public static IEnumerable<ITestData> GetTestData()
    {
        foreach (StructureType type in Enum.GetValues<StructureType>())
        {
            if (type == StructureType.Auto) //We don't test auto. It is covered by the other tests
                continue;

            if (type == StructureType.KeyLength)
                yield return new TestData<string>(type, ["a", "aaa", "aaaa"]);
            else if (type == StructureType.SingleValue)
            {
                yield return new TestData<string>(type, ["value"]);
                yield return new TestData<int>(type, [int.MinValue]);
                yield return new TestData<long>(type, [long.MinValue]);
                yield return new TestData<double>(type, [double.MinValue]);
            }
            else
            {
                yield return new TestData<string>(type, ["item1", "item2", "item3"]);
                yield return new TestData<int>(type, [int.MinValue, 0, int.MaxValue]);
                yield return new TestData<long>(type, [long.MinValue, 0, long.MaxValue]);
                yield return new TestData<double>(type, [double.MinValue, 0, double.MaxValue]);
            }
        }
    }

    public static IEnumerable<ITestData> GetTestVectors()
    {
        foreach (StructureType type in Enum.GetValues<StructureType>())
        {
            switch (type)
            {
                case StructureType.Auto: //No vectors for auto
                    continue;

                case StructureType.KeyLength:
                    // We don't include a length of 1, 2 and 4 to check if uniq length structures emit null buckets correctly
                    yield return new TestData<string>(type, ["aaa", "aaaaa", "aaaaaa", "aaaaaaa", "aaaaaaaa", "aaaaaaaaa", "aaaaaaaaaa"]);
                    break;

                case StructureType.SingleValue:
                    foreach (ITestData data in GetSingleSets(type))
                        yield return data;
                    break;

                case StructureType.Conditional:
                case StructureType.BinarySearch:
                case StructureType.EytzingerSearch:
                case StructureType.HashSetChain:
                case StructureType.HashSetLinear:
                case StructureType.Array:
                    foreach (ITestData data in GetEdgeCaseSets(type))
                        yield return data;
                    foreach (ITestData data in GetSetOfSize(type, 100))
                        yield return data;
                    break;
                default:
                    throw new NotSupportedException("There are no test vectors for " + type);
            }
        }
    }

    public static IEnumerable<ITestData> GetBenchmarkVectors()
    {
        const int benchmarkSize = 1000;

        foreach (StructureType type in Enum.GetValues<StructureType>())
        {
            switch (type)
            {
                //We skip these
                case StructureType.SingleValue:
                case StructureType.Auto:
                    continue;

                case StructureType.KeyLength:
                    yield return new TestData<string>(type, Enumerable.Range(0, benchmarkSize).Select(x => new string('a', x)).ToArray());
                    break;

                case StructureType.Conditional:
                case StructureType.BinarySearch:
                case StructureType.EytzingerSearch:
                case StructureType.HashSetChain:
                case StructureType.HashSetLinear:
                case StructureType.Array:
                    foreach (ITestData data in GetSetOfSize(type, benchmarkSize))
                        yield return data;
                    break;
                default:
                    throw new NotSupportedException("There are no benchmark vectors for " + type);
            }
        }
    }

    private static IEnumerable<ITestData> GetSingleSets(StructureType type)
    {
        // We want to test a single value too of each type. It should result in a special SingleValue structure.
        yield return new TestData<bool>(type, [true]);
        yield return new TestData<sbyte>(type, [1]);
        yield return new TestData<byte>(type, [1]);
        yield return new TestData<char>(type, ['a']);
        yield return new TestData<double>(type, [1.0]);
        yield return new TestData<float>(type, [1f]);
        yield return new TestData<short>(type, [1]);
        yield return new TestData<ushort>(type, [1]);
        yield return new TestData<int>(type, [1]);
        yield return new TestData<uint>(type, [1U]);
        yield return new TestData<long>(type, [1L]);
        yield return new TestData<ulong>(type, [1UL]);
        yield return new TestData<string>(type, ["value"]);
    }

    private static IEnumerable<ITestData> GetEdgeCaseSets(StructureType type)
    {
        // We want to test edge values
        yield return new TestData<bool>(type, [true, false]);
        yield return new TestData<sbyte>(type, [sbyte.MinValue, -1, 0, 1, sbyte.MaxValue]);
        yield return new TestData<byte>(type, [0, 1, byte.MaxValue]);

        //We keep it within ASCII range as C#'s char does not translate to other languages
        yield return new TestData<char>(type, ['\0', 'a', (char)127]);
        yield return new TestData<double>(type, [double.MinValue, 0.0, 1.0, double.MaxValue]);
        yield return new TestData<float>(type, [float.MinValue, -1f, 0f, 1f, float.MaxValue]);
        yield return new TestData<short>(type, [short.MinValue, -1, 0, 1, short.MaxValue]);
        yield return new TestData<ushort>(type, [0, 1, 2, ushort.MaxValue]);
        yield return new TestData<int>(type, [int.MinValue, -1, 0, 1, int.MaxValue]);
        yield return new TestData<uint>(type, [0U, 1U, 2U, uint.MaxValue]);

        //We use -5 and -1 to avoid hash collisions
        yield return new TestData<long>(type, [long.MinValue, -5L, 0L, 1L, long.MaxValue - 1]);

        //We use -1 to avoid hash collisions
        yield return new TestData<ulong>(type, [0UL, 1UL, 2UL, ulong.MaxValue - 10]);
        yield return new TestData<string>(type, ["a", "item", new string('a', 255)]);
    }

    private static IEnumerable<ITestData> GetSetOfSize(StructureType type, int size)
    {
        yield return new TestData<int>(type, Enumerable.Range(0, size).Select(x => x).ToArray());
        yield return new TestData<float>(type, Enumerable.Range(0, size).Select(x => (float)x).ToArray());
        yield return new TestData<string>(type, Enumerable.Range(0, size).Select(x => x.ToString(NumberFormatInfo.InvariantInfo)).ToArray());
    }
}