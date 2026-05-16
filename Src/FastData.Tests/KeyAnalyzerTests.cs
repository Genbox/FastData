using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Enums;
using Genbox.FastData.Internal.Analysis.Data;
using Genbox.FastData.Internal.Analysis.Properties;
using static Genbox.FastData.Internal.Analysis.KeyAnalyzer;

namespace Genbox.FastData.Tests;

public class KeyAnalyzerTests
{
    [Fact]
    public void GetProperties_IsConsecutive_Test()
    {
        Assert.True(GetNumericProperties<char>(new[] { 'a', 'b', 'c' }, false).IsConsecutive);
        Assert.False(GetNumericProperties<char>(new[] { 'a', 'c' }, false).IsConsecutive);

        Assert.True(GetNumericProperties<sbyte>(new sbyte[] { -1, 0, 1 }, false).IsConsecutive);
        Assert.False(GetNumericProperties<sbyte>(new sbyte[] { -1, 1, 2 }, false).IsConsecutive);

        Assert.True(GetNumericProperties<byte>(new byte[] { 1, 2, 3 }, false).IsConsecutive);
        Assert.False(GetNumericProperties<byte>(new byte[] { 1, 3, 4 }, false).IsConsecutive);

        Assert.True(GetNumericProperties<short>(new short[] { 10, 11, 12 }, false).IsConsecutive);
        Assert.False(GetNumericProperties<short>(new short[] { 10, 11, 13 }, false).IsConsecutive);

        Assert.True(GetNumericProperties<ushort>(new ushort[] { 10, 11, 12 }, false).IsConsecutive);
        Assert.False(GetNumericProperties<ushort>(new ushort[] { 10, 11, 13 }, false).IsConsecutive);

        Assert.True(GetNumericProperties<int>(new[] { 100, 101 }, false).IsConsecutive);
        Assert.False(GetNumericProperties<int>(new[] { 100, 102 }, false).IsConsecutive);

        Assert.True(GetNumericProperties<uint>(new[] { 100u, 101u }, false).IsConsecutive);
        Assert.False(GetNumericProperties<uint>(new[] { 100u, 102u }, false).IsConsecutive);

        Assert.True(GetNumericProperties<long>(new[] { long.MaxValue - 2, long.MaxValue - 1, long.MaxValue }, false).IsConsecutive);
        Assert.False(GetNumericProperties<long>(new[] { 1L, 3L, 4L }, false).IsConsecutive);

        Assert.True(GetNumericProperties<ulong>(new[] { 1ul, 2ul, 3ul }, false).IsConsecutive);
        Assert.False(GetNumericProperties<ulong>(new[] { 1ul, 2ul, 4ul }, false).IsConsecutive);

        Assert.False(GetNumericProperties<float>(new[] { 0f, 0.9f, 2f }, false).IsConsecutive);
        Assert.False(GetNumericProperties<double>(new[] { 0.0d, 0.9d, 2.0d }, false).IsConsecutive);
    }

    [Fact]
    public void GetProperties_Density_Test()
    {
        Assert.Equal(1.0f, GetNumericProperties<int>(new[] { 10, 11, 12 }, false).Density);
        Assert.Equal(2.0f / 101.0f, GetNumericProperties<int>(new[] { 0, 100 }, false).Density, 12);
        Assert.Equal(1.0f, GetNumericProperties<int>(new[] { 42 }, false).Density);
    }

    [Fact]
    public void GetNumericProperties_Ranges_SortsInput_Test()
    {
        int[] data = [5, 1, 2, 4, 8];

        NumericKeyProperties<int> props = GetNumericProperties<int>(data, false);

        Assert.Equal(1, props.DataRanges.Min);
        Assert.Equal(8, props.DataRanges.Max);
        Assert.Equal(3, props.DataRanges.Ranges.Count);
        Assert.Equal((1, 2), props.DataRanges.Ranges[0]);
        Assert.Equal((4, 5), props.DataRanges.Ranges[1]);
        Assert.Equal((8, 8), props.DataRanges.Ranges[2]);
        Assert.False(props.IsConsecutive);
    }

    [Fact]
    public void GetNumericProperties_FloatHasZero_Test()
    {
        NumericKeyProperties<float> withZero = GetNumericProperties<float>(new[] { -0.0f, 1.25f }, false);
        NumericKeyProperties<float> withoutZero = GetNumericProperties<float>(new[] { 1.25f, 2.5f }, false);

        Assert.True(withZero.HasZero);
        Assert.False(withoutZero.HasZero);
    }

    [Fact]
    public void GetNumericProperties_DoubleClampsRange_Test()
    {
        NumericKeyProperties<double> props = GetNumericProperties<double>(new[] { 0.0d, double.MaxValue }, true);

        Assert.True(props.HasZero);
        Assert.Equal(ulong.MaxValue, props.Range);
    }

    [Theory]
    [InlineData((object)new[] { "a", "aa", "aaa", "aaaa", "aaaaa", "aaaaaa", "aaaaaaa", "aaaaaaaa" })]
    [InlineData((object)new[] { "aaa", "aaaaa", "aaaaaa", "aaaaaaa", "aaaaaaaa", "aaaaaaaaa", "aaaaaaaaaa" })] //Test inputs that don't start with 1
    [InlineData((object)new[] { "a", "aaa", "aaaa" })] //Test when there is gaps
    [InlineData((object)new[] { "a" })] //Test when there is only one item
    [InlineData((object)new[] { "a", "a", "aaa", "aaa" })] //Test duplicates
    public void GetStringProperties_LengthRanges_Test(string[] data)
    {
        StringKeyProperties res = GetStringProperties(data, false, GeneratorEncoding.Utf16CodeUnits);
        LengthData lengthData = res.LengthData;

        HashSet<int> expectedLengths = [];
        int minLength = int.MaxValue;
        int maxLength = 0;

        foreach (string value in data)
        {
            int length = value.Length;
            expectedLengths.Add(length);
            minLength = Math.Min(minLength, length);
            maxLength = Math.Max(maxLength, length);
        }

        Assert.Equal(expectedLengths.Count == data.Length, lengthData.UniqueLengths);
        Assert.Equal(minLength, lengthData.MinCharLength);
        Assert.Equal(maxLength, lengthData.MaxCharLength);
        Assert.Equal(expectedLengths.Count, CountLengths(lengthData.LengthRanges));

        foreach (int length in expectedLengths)
            Assert.True(ContainsLength(lengthData.LengthRanges, length));
    }

    [Theory]
    [InlineData(new[] { "item1", "item2", "item3", "item4" }, 4, 0)]
    [InlineData(new[] { "1item", "2item", "3item", "4item" }, 0, 4)]
    [InlineData(new[] { "a", "ab", "abc" }, 0, 0)] // The shortest string would become empty, so we don't support it
    [InlineData(new[] { "aa", "aaa", "aaaaa" }, 0, 0)] // If all strings consist of the same character, they will be reduced to nothing, so we don't support it
    [InlineData(new[] { "hello world" }, 0, 0)] // One key should result in no prefix/suffix calculation
    public void GetStringProperties_DeltaData_Test(string[] data, int leftZero, int rightZero)
    {
        StringKeyProperties res = GetStringProperties(data, false, GeneratorEncoding.Utf16CodeUnits);
        Assert.Equal(leftZero, res.DeltaData.Prefix.Length);
        Assert.Equal(rightZero, res.DeltaData.Suffix.Length);
    }

    [Fact]
    public void GetStringProperties_DeltaData_IgnoreCase_Test()
    {
        string[] data = ["Abc1", "Abc2", "Abc3", "abc4"];

        StringKeyProperties ignoreCase = GetStringProperties(data, true, GeneratorEncoding.Utf16CodeUnits);
        StringKeyProperties caseSensitive = GetStringProperties(data, false, GeneratorEncoding.Utf16CodeUnits);

        Assert.Equal("Abc", ignoreCase.DeltaData.Prefix);
        Assert.Equal(string.Empty, caseSensitive.DeltaData.Prefix);
    }

    [Fact]
    public void GetStringProperties_CharRange_Test()
    {
        StringKeyProperties res = GetStringProperties(new[] { "Apple", "banana", "Cherry" }, false, GeneratorEncoding.Utf16CodeUnits);
        CharacterData data = res.CharacterData;
        Assert.Equal('A', data.FirstCharMap.Min);
        Assert.Equal('b', data.FirstCharMap.Max);
        Assert.Equal('a', data.LastCharMap.Min);
        Assert.Equal('y', data.LastCharMap.Max);
    }

    [Fact]
    public void GetStringProperties_CharRange_IgnoreCase_Test()
    {
        StringKeyProperties res = GetStringProperties(new[] { "Apple", "banana", "Cherry" }, true, GeneratorEncoding.Utf16CodeUnits);
        CharacterData data = res.CharacterData;
        Assert.Equal('a', data.FirstCharMap.Min);
        Assert.Equal('c', data.FirstCharMap.Max);
        Assert.Equal('a', data.LastCharMap.Min);
        Assert.Equal('y', data.LastCharMap.Max);
    }

    [Fact]
    public void GetStringProperties_AsciiEarlyExitData_Test()
    {
        (LengthData lengthData, _, CharacterData data) = GetStringProperties(new[] { "ab", "ac", "bd" }, true, GeneratorEncoding.Utf16CodeUnits);
        Assert.Equal(2, lengthData.MinCharLength);
        Assert.Equal(2, lengthData.MaxCharLength);
        Assert.True(data.AllAscii);
    }

    [Fact]
    public void GetStringProperties_CharacterClasses_Test()
    {
        StringKeyProperties res = GetStringProperties(new[] { "A b1$", "c" }, false, GeneratorEncoding.Utf16CodeUnits);
        CharacterClass expected = CharacterClass.Uppercase | CharacterClass.Lowercase | CharacterClass.Number | CharacterClass.Symbol | CharacterClass.Whitespace;

        Assert.Equal(expected, res.CharacterData.CharacterClasses);
    }

    [Fact]
    public void GetStringProperties_AllAscii_False_Test()
    {
        StringKeyProperties res = GetStringProperties(new[] { "abc", "\u0101bc" }, false, GeneratorEncoding.Utf16CodeUnits);

        Assert.False(res.CharacterData.AllAscii);
    }

    private static int CountLengths(DataRanges<int> ranges)
    {
        int count = 0;

        foreach ((int Start, int End) range in ranges.Ranges)
            count += (range.End - range.Start) + 1;

        return count;
    }

    private static bool ContainsLength(DataRanges<int> ranges, int length)
    {
        foreach ((int Start, int End) range in ranges.Ranges)
        {
            if (length >= range.Start && length <= range.End)
                return true;
        }

        return false;
    }
}