using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Internal.Helpers;

namespace Genbox.FastData.Tests;

public class StringHelperTests
{
    [Fact]
    public void GetLengthFunc_UsesEncodingLength()
    {
        const string value = "a\u0101";

        Assert.Equal(Encoding.ASCII.GetByteCount(value), StringHelper.GetLengthFunc(GeneratorEncoding.AsciiBytes)(value));
        Assert.Equal(Encoding.UTF8.GetByteCount(value), StringHelper.GetLengthFunc(GeneratorEncoding.Utf8Bytes)(value));
        Assert.Equal(Encoding.Unicode.GetByteCount(value), StringHelper.GetLengthFunc(GeneratorEncoding.Utf16Bytes)(value));
        Assert.Equal(value.Length, StringHelper.GetLengthFunc(GeneratorEncoding.Utf16CodeUnits)(value));
    }

    [Fact]
    public void GetBytesFunc_ReturnsEncodingBytes()
    {
        const string value = "Hello";

        byte[] ascii = StringHelper.GetBytesFunc(GeneratorEncoding.AsciiBytes)(value);
        Assert.Equal(Encoding.ASCII.GetBytes(value), ascii);

        byte[] utf8 = StringHelper.GetBytesFunc(GeneratorEncoding.Utf8Bytes)(value);
        Assert.Equal(Encoding.UTF8.GetBytes(value), utf8);

        byte[] utf16 = StringHelper.GetBytesFunc(GeneratorEncoding.Utf16Bytes)(value);
        Assert.Equal(Encoding.Unicode.GetBytes(value), utf16);
    }

    [Fact]
    public void GetSize_ReturnsExpectedByteWidth()
    {
        Assert.Equal(1, StringHelper.GetSize(GeneratorEncoding.AsciiBytes));
        Assert.Equal(1, StringHelper.GetSize(GeneratorEncoding.Utf8Bytes));
        Assert.Equal(2, StringHelper.GetSize(GeneratorEncoding.Utf16Bytes));
        Assert.Equal(2, StringHelper.GetSize(GeneratorEncoding.Utf16CodeUnits));
    }

    [Fact]
    public void GetStringComparer_ReturnsOrdinalVariants()
    {
        Assert.True(StringHelper.GetStringComparer(true).Equals("alpha", "ALPHA"));
        Assert.False(StringHelper.GetStringComparer(false).Equals("alpha", "ALPHA"));
    }

    [Fact]
    public void UnsupportedEncoding_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => StringHelper.GetLengthFunc(GeneratorEncoding.Unknown));
        Assert.Throws<InvalidOperationException>(() => StringHelper.GetBytesFunc(GeneratorEncoding.Unknown));
        Assert.Throws<InvalidOperationException>(() => StringHelper.GetSize(GeneratorEncoding.Unknown));
    }
}