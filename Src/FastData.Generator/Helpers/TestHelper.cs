using System.Globalization;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Enums;

namespace Genbox.FastData.Generator.Helpers;

public static class TestHelper
{
    public static bool TryGenerate(IGenerator generator, StructureType structureType, object[] data, out GeneratorSpec spec)
    {
        DataType dataType = (DataType)Enum.Parse(typeof(DataType), data[0].GetType().Name);

        string identifier = $"{structureType}_{dataType}_{data.Length}";

        if (FastDataGenerator.TryGenerate(data, new FastDataConfig(structureType), generator, out string? source))
        {
            spec = new GeneratorSpec(identifier, dataType, source);
            return true;
        }

        spec = default;
        return false;
    }

    public static IEnumerable<object[]> GetSingleSets()
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

    public static IEnumerable<object[]> GetEdgeCaseSets()
    {
        // We want to test edge values
        yield return [true, false];
        yield return [sbyte.MinValue, (sbyte)-1, (sbyte)0, (sbyte)1, sbyte.MaxValue];
        yield return [(byte)0, (byte)1, byte.MaxValue];
        yield return [char.MinValue, 'a', char.MaxValue];
        yield return [double.MinValue, 0.0, 1.0, double.MaxValue];
        yield return [float.MinValue, -1f, 0f, 1f, float.MaxValue];
        yield return [short.MinValue, (short)-1, (short)0, (short)1, short.MaxValue];
        yield return [(ushort)0, (ushort)1, (ushort)2, ushort.MaxValue];
        yield return [int.MinValue, -1, 0, 1, int.MaxValue];
        yield return [0U, 1U, 2U, uint.MaxValue];
        yield return [long.MinValue, -1L, 0L, 1L, long.MaxValue];
        yield return [0UL, 1UL, 2UL, ulong.MaxValue];
        yield return ["a", "item", new string('a', 255)];
    }

    public static IEnumerable<object[]> GetUniqueLengthSets()
    {
        // We want to attempt strings with unique lengths
        // We don't include a length of 1, 2 and 4 to check if uniq length structures emit null buckets correctly
        yield return ["aaa", "aaaaa", "aaaaaa", "aaaaaaa", "aaaaaaaa", "aaaaaaaaa", "aaaaaaaaaa"];
    }

    public static IEnumerable<object[]> GetLargeSets()
    {
        // We want to test large inputs too
        yield return Enumerable.Range(0, 100).Select(x => (sbyte)x).Cast<object>().ToArray();
        yield return Enumerable.Range(0, 100).Select(x => (byte)x).Cast<object>().ToArray();
        yield return Enumerable.Range(0, 100).Select(x => (short)x).Cast<object>().ToArray();
        yield return Enumerable.Range(0, 100).Select(x => (ushort)x).Cast<object>().ToArray();
        yield return Enumerable.Range(0, 100).Select(x => x).Cast<object>().ToArray();
        yield return Enumerable.Range(0, 100).Select(x => (uint)x).Cast<object>().ToArray();
        yield return Enumerable.Range(0, 100).Select(x => (long)x).Cast<object>().ToArray();
        yield return Enumerable.Range(0, 100).Select(x => (ulong)x).Cast<object>().ToArray();
        yield return Enumerable.Range(0, 100).Select(x => (float)x).Cast<object>().ToArray();
        yield return Enumerable.Range(0, 100).Select(x => (double)x).Cast<object>().ToArray();
        yield return Enumerable.Range(0, 100).Select(x => x.ToString(NumberFormatInfo.InvariantInfo)).Cast<object>().ToArray();
    }

    public static IEnumerable<object[]> GetAllSets() => GetSingleSets()
                                                        .Concat(GetEdgeCaseSets())
                                                        .Concat(GetUniqueLengthSets())
                                                        .Concat(GetLargeSets());

    public static IEnumerable<(StructureType, object[])> GetTestData()
    {
        foreach (StructureType type in Enum.GetValues(typeof(StructureType)))
        {
            if (type == StructureType.Auto) //We don't test auto. It is covered by the other tests
                continue;

            if (type == StructureType.KeyLength)
            {
                foreach (object[] data in GetUniqueLengthSets())
                    yield return (type, data);
            }
            else if (type == StructureType.SingleValue)
            {
                foreach (object[] data in GetSingleSets())
                    yield return (type, data);
            }
            else if (type == StructureType.PerfectHashGPerf)
            {
                //GPerf only supports strings
                yield return (type, ["a", "b"]); //Minimum test case
                yield return (type, ["aaaaaaaaaa", "bbbbbbbbbb", "cccccccccc"]); //Same length (and longer than 1)
                yield return (type, ["item1", "item2", "item3", "item4"]); //Only differ on 1 char
                yield return (type, ["1", "2", "a", "aa", "aaa", "item", new string('a', 255)]); //Test long strings
            }
            else
            {
                foreach (object[] data in GetEdgeCaseSets())
                    yield return (type, data);
            }
        }
    }
}