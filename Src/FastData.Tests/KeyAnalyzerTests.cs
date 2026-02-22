using Genbox.FastData.Enums;
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

        Assert.True(GetNumericProperties<float>(new[] { 0.5f, 1.5f, 2.5f }, false).IsConsecutive);
        Assert.False(GetNumericProperties<float>(new[] { 0f, 0.9f, 2f }, false).IsConsecutive);

        Assert.True(GetNumericProperties<double>(new[] { 0.5d, 1.5d, 2.5d }, false).IsConsecutive);
        Assert.False(GetNumericProperties<double>(new[] { 0d, 0.9d, 2d }, false).IsConsecutive);
    }

    [Fact]
    public void GetProperties_IsConsecutive_Sorted_Test()
    {
        Assert.True(GetNumericProperties<float>(new[] { 0.5f, 1.5f, 2.5f }, true).IsConsecutive);
        Assert.False(GetNumericProperties<float>(new[] { 0.5f, 1.5f, 3.5f }, true).IsConsecutive);

        Assert.True(GetNumericProperties<double>(new[] { 0.5d, 1.5d, 2.5d }, true).IsConsecutive);
        Assert.False(GetNumericProperties<double>(new[] { 0.5d, 1.5d, 3.5d }, true).IsConsecutive);
    }

    [Fact]
    public void GetProperties_Density_Test()
    {
        Assert.Equal(1.0d, GetNumericProperties<int>(new[] { 10, 11, 12 }, false).Density);
        Assert.Equal(2.0d / 101.0d, GetNumericProperties<int>(new[] { 0, 100 }, false).Density, 12);
        Assert.Equal(1.0d, GetNumericProperties<int>(new[] { 42 }, false).Density);
    }

    [Theory]
    [InlineData((object)new[] { "a", "aa", "aaa", "aaaa", "aaaaa", "aaaaaa", "aaaaaaa", "aaaaaaaa" })]
    [InlineData((object)new[] { "aaa", "aaaaa", "aaaaaa", "aaaaaaa", "aaaaaaaa", "aaaaaaaaa", "aaaaaaaaaa" })] //Test inputs that don't start with 1
    [InlineData((object)new[] { "a", "aaa", "aaaa" })] //Test when there is gaps
    [InlineData((object)new[] { "a" })] //Test when there is only one item
    [InlineData((object)new[] { "a", "a", "aaa", "aaa" })] //Test duplicates
    public void GetStringProperties_LengthMap_Test(string[] data)
    {
        StringKeyProperties res = GetStringProperties(data, false, false, GeneratorEncoding.UTF16);
        LengthBitArray map = res.LengthData.LengthMap;
        Assert.Equal(data.Distinct().Count(), map.BitCount);

        foreach (string str in data)
        {
            Assert.True(map.Get((uint)str.Length));
        }

        Assert.Equal((uint)data.Min(x => x.Length), res.LengthData.LengthMap.Min);
        Assert.Equal((uint)data.Max(x => x.Length), res.LengthData.LengthMap.Max);
    }

    [Theory]
    [InlineData(new[] { "item1", "item2", "item3", "item4" }, 4, 0)]
    [InlineData(new[] { "1item", "2item", "3item", "4item" }, 0, 4)]
    [InlineData(new[] { "a", "ab", "abc" }, 0, 0)] // The shortest string would become empty, so we don't support it
    [InlineData(new[] { "aa", "aaa", "aaaaa" }, 0, 0)] // If all strings consist of the same character, they will be reduced to nothing, so we don't support it
    [InlineData(new[] { "hello world" }, 0, 0)] // One key should result in no prefix/suffix calculation
    public void GetStringProperties_DeltaData_Test(string[] data, int leftZero, int rightZero)
    {
        StringKeyProperties res = GetStringProperties(data, true, false, GeneratorEncoding.UTF16);
        Assert.Equal(leftZero, res.DeltaData.Prefix.Length);
        Assert.Equal(rightZero, res.DeltaData.Suffix.Length);
    }

    [Fact]
    public void GetStringProperties_CharRange_Test()
    {
        StringKeyProperties res = GetStringProperties(new[] { "Apple", "banana", "Cherry" }, false, false, GeneratorEncoding.UTF16);
        CharacterData data = res.CharacterData;
        Assert.Equal('A', data.FirstCharMap.Min);
        Assert.Equal('b', data.FirstCharMap.Max);
        Assert.Equal('a', data.LastCharMap.Min);
        Assert.Equal('y', data.LastCharMap.Max);
    }

    [Fact]
    public void GetStringProperties_CharRange_IgnoreCase_Test()
    {
        StringKeyProperties res = GetStringProperties(new[] { "Apple", "banana", "Cherry" }, false, true, GeneratorEncoding.UTF16);
        CharacterData data = res.CharacterData;
        Assert.Equal('a', data.FirstCharMap.Min);
        Assert.Equal('c', data.FirstCharMap.Max);
        Assert.Equal('a', data.LastCharMap.Min);
        Assert.Equal('y', data.LastCharMap.Max);
    }

    [Fact]
    public void GetStringProperties_AsciiEarlyExitData_Test()
    {
        (LengthData lengthData, _, CharacterData data) = GetStringProperties(new[] { "ab", "ac", "bd" }, false, true, GeneratorEncoding.UTF16);
        Assert.True(lengthData.LengthMap.HasEven);
        Assert.False(lengthData.LengthMap.HasOdd);
        Assert.True(data.AllAscii);
        Assert.Equal(2u, lengthData.CharDivisor);
        Assert.Equal(4u, lengthData.ByteDivisor);
    }

    [Fact]
    public void GetStringProperties_CommonDivisor_None_Test()
    {
        (LengthData lengthData, _, _) = GetStringProperties(new[] { "abc", "abcd" }, false, false, GeneratorEncoding.UTF16);
        Assert.Equal(0u, lengthData.CharDivisor);
        Assert.Equal(0u, lengthData.ByteDivisor);
    }
}