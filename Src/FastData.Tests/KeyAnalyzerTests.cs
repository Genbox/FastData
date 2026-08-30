using Genbox.FastData.Enums;
using Genbox.FastData.Generators.Enums;
using Genbox.FastData.Internal.Analysis.Data;
using Genbox.FastData.Internal.Analysis.Properties;
using static Genbox.FastData.Internal.Analysis.KeyAnalyzer;

namespace Genbox.FastData.Tests;

public class KeyAnalyzerTests
{
    private static readonly ReadOnlyMemory<char> _consecutiveChars = new[] { 'a', 'b', 'c' };
    private static readonly ReadOnlyMemory<char> _nonConsecutiveChars = new[] { 'a', 'c' };
    private static readonly ReadOnlyMemory<int> _consecutiveInts = new[] { 100, 101 };
    private static readonly ReadOnlyMemory<int> _nonConsecutiveInts = new[] { 100, 102 };
    private static readonly ReadOnlyMemory<uint> _consecutiveUInts = new[] { 100U, 101U };
    private static readonly ReadOnlyMemory<uint> _nonConsecutiveUInts = new[] { 100U, 102U };
    private static readonly ReadOnlyMemory<long> _nonConsecutiveLongs = new[] { 1L, 3L, 4L };
    private static readonly ReadOnlyMemory<ulong> _consecutiveUlongs = new[] { 1UL, 2UL, 3UL };
    private static readonly ReadOnlyMemory<ulong> _nonConsecutiveUlongs = new[] { 1UL, 2UL, 4UL };
    private static readonly ReadOnlyMemory<float> _nonConsecutiveSingles = new[] { 0F, 0.9F, 2F };
    private static readonly ReadOnlyMemory<double> _nonConsecutiveDoubles = new[] { 0D, 0.9D, 2D };
    private static readonly ReadOnlyMemory<int> _denseInts = new[] { 10, 11, 12 };
    private static readonly ReadOnlyMemory<int> _sparseInts = new[] { 0, 100 };
    private static readonly ReadOnlyMemory<int> _singleInt = new[] { 42 };
    private static readonly ReadOnlyMemory<float> _nonzeroSingles = new[] { 1.25F, 2.5F };

    [Fact]
    public void GetProperties_IsConsecutive_Test()
    {
        Assert.True(GetNumericProperties<char>(_consecutiveChars).IsConsecutive);
        Assert.False(GetNumericProperties<char>(_nonConsecutiveChars).IsConsecutive);

        Assert.True(GetNumericProperties<sbyte>(new sbyte[] { -1, 0, 1 }).IsConsecutive);
        Assert.False(GetNumericProperties<sbyte>(new sbyte[] { -1, 1, 2 }).IsConsecutive);

        Assert.True(GetNumericProperties<byte>(new byte[] { 1, 2, 3 }).IsConsecutive);
        Assert.False(GetNumericProperties<byte>(new byte[] { 1, 3, 4 }).IsConsecutive);

        Assert.True(GetNumericProperties<short>(new short[] { 10, 11, 12 }).IsConsecutive);
        Assert.False(GetNumericProperties<short>(new short[] { 10, 11, 13 }).IsConsecutive);

        Assert.True(GetNumericProperties<ushort>(new ushort[] { 10, 11, 12 }).IsConsecutive);
        Assert.False(GetNumericProperties<ushort>(new ushort[] { 10, 11, 13 }).IsConsecutive);

        Assert.True(GetNumericProperties<int>(_consecutiveInts).IsConsecutive);
        Assert.False(GetNumericProperties<int>(_nonConsecutiveInts).IsConsecutive);

        Assert.True(GetNumericProperties<uint>(_consecutiveUInts).IsConsecutive);
        Assert.False(GetNumericProperties<uint>(_nonConsecutiveUInts).IsConsecutive);

        Assert.True(GetNumericProperties<long>(new[] { long.MaxValue - 2, long.MaxValue - 1, long.MaxValue }).IsConsecutive);
        Assert.False(GetNumericProperties<long>(_nonConsecutiveLongs).IsConsecutive);

        Assert.True(GetNumericProperties<ulong>(_consecutiveUlongs).IsConsecutive);
        Assert.False(GetNumericProperties<ulong>(_nonConsecutiveUlongs).IsConsecutive);

        Assert.False(GetNumericProperties<float>(_nonConsecutiveSingles).IsConsecutive);
        Assert.False(GetNumericProperties<double>(_nonConsecutiveDoubles).IsConsecutive);
    }

    [Fact]
    public void GetProperties_Density_Test()
    {
        Assert.Equal(1.0f, GetNumericProperties<int>(_denseInts).Density);
        Assert.Equal(2.0f / 101.0f, GetNumericProperties<int>(_sparseInts).Density, 12);
        Assert.Equal(1.0f, GetNumericProperties<int>(_singleInt).Density);
    }

    [Fact]
    public void GetNumericProperties_FloatHasZero_Test()
    {
        NumericKeyProperties<float> withZero = GetNumericProperties<float>(new[] { -0.0f, 1.25f });
        NumericKeyProperties<float> withoutZero = GetNumericProperties<float>(_nonzeroSingles);

        Assert.True(withZero.HasZero);
        Assert.False(withoutZero.HasZero);
    }

    [Fact]
    public void GetNumericProperties_DoubleClampsRange_Test()
    {
        NumericKeyProperties<double> props = GetNumericProperties<double>(new[] { 0.0d, double.MaxValue });

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
        ArgumentNullException.ThrowIfNull(data);

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
        Assert.Equal(minLength * 2, lengthData.MinByteLength); // Utf16CodeUnits: 2 bytes per char
        Assert.Equal(maxLength * 2, lengthData.MaxByteLength);
        Assert.Equal(expectedLengths.Count, CountLengths(lengthData.LengthRanges));

        foreach (int length in expectedLengths)
            Assert.True(ContainsLength(lengthData.LengthRanges, length));
    }

    [Theory]
    [InlineData(GeneratorEncoding.Utf8Bytes, "é", 2)]
    [InlineData(GeneratorEncoding.Utf8Bytes, "😀", 4)]
    [InlineData(GeneratorEncoding.Utf16Bytes, "é", 2)]
    [InlineData(GeneratorEncoding.Utf16Bytes, "😀", 4)]
    [InlineData(GeneratorEncoding.Utf16CodeUnits, "é", 1)]
    [InlineData(GeneratorEncoding.Utf16CodeUnits, "😀", 2)]
    public void GetStringProperties_UsesEncodingLength(GeneratorEncoding encoding, string value, int expectedLength)
    {
        StringKeyProperties properties = GetStringProperties([value], false, encoding);

        Assert.Equal(expectedLength, properties.LengthData.LengthRanges.Min);
        Assert.Equal(expectedLength, properties.LengthData.LengthRanges.Max);
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
        Assert.Equal(4, lengthData.MinByteLength); // Utf16CodeUnits: 2 bytes per char, "ab" = 4 bytes
        Assert.Equal(4, lengthData.MaxByteLength);
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