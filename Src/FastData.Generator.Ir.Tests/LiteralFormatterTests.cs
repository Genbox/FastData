using System.Globalization;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Ir.Emission;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.Tests;

public class LiteralFormatterTests
{
    [Theory]
    [InlineData(GeneratorEncoding.AsciiBytes, "A\0z", new byte[] { 65, 0, 122 })]
    [InlineData(GeneratorEncoding.Utf8Bytes, "é😀", new byte[] { 195, 169, 240, 159, 152, 128 })]
    public void ByteEncodingPreservesEncodedUnits(GeneratorEncoding encoding, string value, byte[] expected) =>
        Assert.Equal(expected, LiteralFormatter.EncodeBytes(value, encoding));

    [Theory]
    [InlineData(false, "\"\\\"\\\\<10><55357><56832>\"")]
    [InlineData(true, "\"\\\"\\\\<10><128512>\"")]
    public void QuotingEscapesDelimitersAndFormatsUnicode(bool unicodeScalars, string expected)
    {
        string actual = LiteralFormatter.Quote("\"\\\n😀", '"',
            static (text, value) => text.Append('<').Append(LiteralFormatter.InvariantNumber(value)).Append('>'), unicodeScalars);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CharacterQuotingEscapesTheSelectedDelimiter()
    {
        string actual = LiteralFormatter.Quote("'\"\\", '\'',
            static (text, value) => text.Append('<').Append(LiteralFormatter.InvariantNumber(value)).Append('>'));

        Assert.Equal("'\\'\"\\\\'", actual);
    }

    [Theory]
    [InlineData(IrTextEncoding.Utf8)]
    [InlineData(IrTextEncoding.Utf16)]
    public void PackedStringsReachTheFormatterWithTheirExactCharacters(IrTextEncoding encoding)
    {
        string?[] strings = [null, string.Empty, "ASCII", "\u00e9\U0001f600", new string('x', 1025), encoding == IrTextEncoding.Utf16 ? "\ud800" : "end"];
        IrPackedStringArrayLiteral packed = new IrText(encoding).PackStrings(strings);
        InspectingFormatter formatter = new InspectingFormatter();
        Assert.Equal(strings.Select(text => text ?? "<null>"), formatter.Elements(packed), StringComparer.Ordinal);
    }

    [Fact]
    public void PackedIntegersPreserveTheirValuesAndDeclaredWidths()
    {
        (IrArrayLiteralBase Array, string Expected)[] cases =
        [
            (new IrPackedArrayLiteral<sbyte>([sbyte.MinValue]), "I8:-128:element"),
            (new IrPackedArrayLiteral<byte>([byte.MaxValue]), "U8:255:element"),
            (new IrPackedArrayLiteral<short>([short.MinValue]), "I16:-32768:element"),
            (new IrPackedArrayLiteral<ushort>([ushort.MaxValue]), "U16:65535:element"),
            (new IrPackedArrayLiteral<int>([int.MinValue]), "I32:-2147483648:element"),
            (new IrPackedArrayLiteral<uint>([uint.MaxValue]), "U32:4294967295:element"),
            (new IrPackedArrayLiteral<long>([long.MinValue]), "I64:-9223372036854775808:element"),
            (new IrPackedArrayLiteral<ulong>([ulong.MaxValue]), "U64:18446744073709551615:element")
        ];
        InspectingFormatter formatter = new InspectingFormatter();
        foreach ((IrArrayLiteralBase array, string expected) in cases)
            Assert.Equal(expected, Assert.Single(formatter.Elements(array)));

        Assert.Equal("I8:-128:expression", formatter.Literal(new IrI8Literal(sbyte.MinValue)));
        Assert.Equal("I8:-128:element", formatter.Element(new IrI8Literal(sbyte.MinValue)));
    }

    [Fact]
    public void PackedFloatingPointValuesRetainTheirBits()
    {
        InspectingFormatter formatter = new InspectingFormatter();
        IrPackedArrayLiteral<float> singles = new IrPackedArrayLiteral<float>([BitConverter.UInt32BitsToSingle(0x80000000), BitConverter.UInt32BitsToSingle(0x7FC12345)]);
        IrPackedArrayLiteral<double> doubles = new IrPackedArrayLiteral<double>([BitConverter.UInt64BitsToDouble(0x8000000000000000), BitConverter.UInt64BitsToDouble(0x7FF8123456789ABC)]);
        Assert.Equal(["80000000", "7FC12345"], formatter.Elements(singles), StringComparer.Ordinal);
        Assert.Equal(["8000000000000000", "7FF8123456789ABC"], formatter.Elements(doubles), StringComparer.Ordinal);
    }

    private sealed class InspectingFormatter : LiteralFormatter
    {
        public override string Type(IrType type) => type.Kind.ToString();
        protected override string FormatString(ReadOnlySpan<char> value) => value.ToString();
        protected override string FormatNull(IrType type) => "<null>";
        protected override string Signed(long value, IrTypeKind kind, bool element) => $"{kind}:{InvariantNumber(value)}:{(element ? "element" : "expression")}";
        protected override string Unsigned(ulong value, IrTypeKind kind, bool element) => $"{kind}:{InvariantNumber(value)}:{(element ? "element" : "expression")}";
        protected override string Format(float value) => BitConverter.SingleToUInt32Bits(value).ToString("X8", CultureInfo.InvariantCulture);
        protected override string Format(double value) => BitConverter.DoubleToUInt64Bits(value).ToString("X16", CultureInfo.InvariantCulture);
        protected override string FormatCharacter(uint value, IrCharacterKind kind) => throw new NotSupportedException();
        protected override string Aggregate(IrLiteral literal) => throw new NotSupportedException();
    }
}