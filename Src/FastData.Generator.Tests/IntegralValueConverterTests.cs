using Genbox.FastData.Generator.Helpers;

namespace Genbox.FastData.Generator.Tests;

public class IntegralValueConverterTests
{
    [Theory]
    [MemberData(nameof(GetSuccessfulConversions))]
    public void ConvertCheckedReturnsExactTargetBoxType(object value, Type targetType, object expected)
    {
        object result = IntegralValueConverter.ConvertChecked(value, targetType);

        Assert.Equal(targetType, result.GetType());
        Assert.Equal(expected, result);
    }

    [Theory]
    [MemberData(nameof(GetOverflowConversions))]
    public void ConvertCheckedThrowsOnOverflow(object value, Type targetType)
    {
        Assert.Throws<OverflowException>(() => IntegralValueConverter.ConvertChecked(value, targetType));
    }

    [Theory]
    [InlineData(typeof(byte))]
    [InlineData(typeof(uint))]
    [InlineData(typeof(ulong))]
    public void ConvertCheckedRejectsNegativeValuesForUnsignedTargets(Type targetType)
    {
        Assert.Throws<OverflowException>(() => IntegralValueConverter.ConvertChecked(-1, targetType));
    }

    [Theory]
    [MemberData(nameof(GetUnsupportedSourceValues))]
    public void ConvertCheckedRejectsUnsupportedSource(object value)
    {
        Assert.Throws<ArgumentException>(() => IntegralValueConverter.ConvertChecked(value, typeof(int)));
    }

    [Fact]
    public void ConvertCheckedRejectsNullValue()
    {
        Assert.Throws<ArgumentNullException>(() => IntegralValueConverter.ConvertChecked(null!, typeof(int)));
    }

    [Fact]
    public void ConvertCheckedRejectsNullTargetType()
    {
        Assert.Throws<ArgumentNullException>(() => IntegralValueConverter.ConvertChecked(1, null!));
    }

    public static TheoryData<object, Type, object> GetSuccessfulConversions() => new TheoryData<object, Type, object>
    {
        { (sbyte)-1, typeof(short), (short)-1 },
        { (short)255, typeof(byte), (byte)255 },
        { 65, typeof(char), 'A' },
        { 'A', typeof(uint), 65U },
        { (byte)127, typeof(sbyte), (sbyte)127 },
        { uint.MaxValue, typeof(long), (long)uint.MaxValue },
        { long.MinValue, typeof(long), long.MinValue },
        { long.MaxValue, typeof(ulong), (ulong)long.MaxValue },
        { ulong.MaxValue, typeof(ulong), ulong.MaxValue }
    };

    public static TheoryData<object, Type> GetOverflowConversions() => new TheoryData<object, Type>
    {
        { 256, typeof(byte) },
        { -129, typeof(sbyte) },
        { uint.MaxValue, typeof(int) },
        { ulong.MaxValue, typeof(long) },
        { (char)256, typeof(byte) }
    };

    public static TheoryData<object> GetUnsupportedSourceValues() =>
    [
        1.0f,
        "1",
        true,
        SampleEnum.Value,
        (nint)1
    ];

    private enum SampleEnum
    {
        Value
    }
}