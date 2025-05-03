using System.Globalization;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;

namespace Genbox.FastData.InternalShared;

public static class TestVectorHelper
{
    public static bool TryGenerate(Func<string, ICodeGenerator> generatorFunc, StructureType structureType, object[] data, out GeneratorSpec spec)
    {
        DataType dataType = (DataType)Enum.Parse(typeof(DataType), data[0].GetType().Name);

        string identifier = $"{structureType}_{dataType}_{data.Length}";

        if (FastDataGenerator.TryGenerate(data, new FastDataConfig(structureType), generatorFunc(identifier), out string? source))
        {
            spec = new GeneratorSpec(identifier, source);
            return true;
        }

        spec = default;
        return false;
    }

    public static IEnumerable<(StructureType, object[])> GetTestData()
    {
        foreach (StructureType type in Enum.GetValues(typeof(StructureType)))
        {
            if (type == StructureType.Auto) //We don't test auto. It is covered by the other tests
                continue;

            if (type == StructureType.KeyLength)
                yield return (type, ["a", "aaa", "aaaa"]);
            else if (type == StructureType.SingleValue)
            {
                yield return (type, ["value"]);
                yield return (type, [int.MinValue]);
                yield return (type, [long.MinValue]);
                yield return (type, [double.MinValue]);
            }
            else if (type == StructureType.PerfectHashGPerf)
                yield return (type, ["item1", "item2", "item3", "item4"]);
            else
            {
                yield return (type, ["item1", "item2", "item3"]);
                yield return (type, [int.MinValue, 0, int.MaxValue]);
                yield return (type, [long.MinValue, (long)0, long.MaxValue]);
                yield return (type, [double.MinValue, (double)0, double.MaxValue]);
            }
        }
    }

    public static IEnumerable<(StructureType, object[])> GetTestVectors()
    {
        foreach (StructureType type in Enum.GetValues(typeof(StructureType)))
        {
            switch (type)
            {
                case StructureType.Auto: //No vectors for auto
                    continue;

                case StructureType.KeyLength:
                    // We don't include a length of 1, 2 and 4 to check if uniq length structures emit null buckets correctly
                    yield return (type, ["aaa", "aaaaa", "aaaaaa", "aaaaaaa", "aaaaaaaa", "aaaaaaaaa", "aaaaaaaaaa"]);
                    break;

                case StructureType.SingleValue:
                    foreach (object[] data in GetSingleSets())
                    {
                        yield return (type, data);
                    }
                    break;

                case StructureType.PerfectHashGPerf:
                    yield return (type, ["a", "b"]); //Minimum test case
                    yield return (type, ["aaaaaaaaaa", "bbbbbbbbbb", "cccccccccc"]); //Same length (and longer than 1)
                    yield return (type, ["item1", "item2", "item3", "item4"]); //Only differ on 1 char
                    yield return (type, ["1", "2", "a", "aa", "aaa", "item", new string('a', 255)]); //Test long strings
                    break;

                case StructureType.PerfectHashBruteForce: //We've kept to a small set since larger ones will just hit timeout
                    foreach (object[] data in GetEdgeCaseSets())
                    {
                        yield return (type, data);
                    }
                    break;

                case StructureType.Conditional:
                case StructureType.BinarySearch:
                case StructureType.EytzingerSearch:
                case StructureType.HashSetChain:
                case StructureType.HashSetLinear:
                case StructureType.Array:
                    foreach (object[] data in GetEdgeCaseSets())
                    {
                        yield return (type, data);
                    }

                    foreach (object[] data in GetSetOfSize(100))
                    {
                        yield return (type, data);
                    }
                    break;
                default:
                    throw new NotSupportedException("There are no test vectors for " + type);
            }
        }
    }

    public static IEnumerable<(StructureType, object[])> GetBenchmarkVectors()
    {
        const int benchmarkSize = 1000;

        foreach (StructureType type in Enum.GetValues(typeof(StructureType)))
        {
            switch (type)
            {
                //We skip these
                case StructureType.SingleValue:
                case StructureType.Auto:
                    continue;

                case StructureType.KeyLength:
                    yield return (type, Enumerable.Range(0, benchmarkSize).Select(x => new string('a', x)).ToArray<object>());
                    break;

                case StructureType.PerfectHashGPerf:
                    yield return (type, Enumerable.Range(0, benchmarkSize).Select(x => "item" + x).ToArray<object>());
                    break;

                case StructureType.PerfectHashBruteForce:
                    foreach (object[] data in GetSetOfSize(10))
                    {
                        yield return (type, data);
                    }
                    break;

                case StructureType.Conditional:
                case StructureType.BinarySearch:
                case StructureType.EytzingerSearch:
                case StructureType.HashSetChain:
                case StructureType.HashSetLinear:
                case StructureType.Array:
                    foreach (object[] data in GetSetOfSize(benchmarkSize))
                    {
                        yield return (type, data);
                    }
                    break;
                default:
                    throw new NotSupportedException("There are no benchmark vectors for " + type);
            }
        }
    }

    private static IEnumerable<object[]> GetSingleSets()
    {
        // We want to test a single value too of each type. It should result in a special SingleValue structure.
        yield return [true];
        yield return [(sbyte)1];
        yield return [(byte)1];
        yield return ['a'];
        yield return [1.0];
        yield return [1f];
        yield return [(short)1];
        yield return [(ushort)1];
        yield return [1];
        yield return [1U];
        yield return [1L];
        yield return [1UL];
        yield return ["value"];
    }

    private static IEnumerable<object[]> GetEdgeCaseSets()
    {
        // We want to test edge values
        yield return [true, false];
        yield return [sbyte.MinValue, (sbyte)-1, (sbyte)0, (sbyte)1, sbyte.MaxValue];
        yield return [(byte)0, (byte)1, byte.MaxValue];
        yield return ['\0', 'a', (char)127]; //We keep it within ASCII range as C#'s char does not translate to other languages
        yield return [double.MinValue, 0.0, 1.0, double.MaxValue];
        yield return [float.MinValue, -1f, 0f, 1f, float.MaxValue];
        yield return [short.MinValue, (short)-1, (short)0, (short)1, short.MaxValue];
        yield return [(ushort)0, (ushort)1, (ushort)2, ushort.MaxValue];
        yield return [int.MinValue, -1, 0, 1, int.MaxValue];
        yield return [0U, 1U, 2U, uint.MaxValue];
        yield return [long.MinValue, -5L, 0L, 1L, long.MaxValue - 1]; //We use -5 and -1 to avoid hash collisions
        yield return [0UL, 1UL, 2UL, ulong.MaxValue - 10]; //We use -1 to avoid hash collisions
        yield return ["a", "item", new string('a', 255)];
    }

    private static IEnumerable<object[]> GetSetOfSize(int size)
    {
        yield return Enumerable.Range(0, size).Select(x => x).Cast<object>().ToArray();
        yield return Enumerable.Range(0, size).Select(x => (float)x).Cast<object>().ToArray();
        yield return Enumerable.Range(0, size).Select(x => x.ToString(NumberFormatInfo.InvariantInfo)).Cast<object>().ToArray();
    }
}