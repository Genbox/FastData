using Genbox.FastData.Generator.Ir.Emission;
using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.Tests;

public class LiteralFormatterTests
{
    [Theory]
    [InlineData(IrTextEncoding.Utf8)]
    [InlineData(IrTextEncoding.Utf16)]
    public void PackedStringsReachTheFormatterWithTheirExactCharacters(IrTextEncoding encoding)
    {
        string?[] strings = [null, string.Empty, "ASCII", "\u00e9\U0001f600", new string('x', 1025), encoding == IrTextEncoding.Utf16 ? "\ud800" : "end"];
        IrPackedStringArrayLiteral packed = new IrText(encoding).PackStrings(strings);
        StringFormatter formatter = new StringFormatter();
        Assert.Equal(strings.Select(text => text ?? "<null>"), formatter.Elements(packed), StringComparer.Ordinal);
    }

    private sealed class StringFormatter : LiteralFormatter
    {
        protected override string FormatString(ReadOnlySpan<char> value) => value.ToString();
        protected override string FormatNull(IrType type) => "<null>";
        protected override string Format(bool value) => throw new NotSupportedException();
        protected override string Format(sbyte value, bool element) => throw new NotSupportedException();
        protected override string Format(byte value, bool element) => throw new NotSupportedException();
        protected override string Format(short value, bool element) => throw new NotSupportedException();
        protected override string Format(ushort value, bool element) => throw new NotSupportedException();
        protected override string Format(int value) => throw new NotSupportedException();
        protected override string Format(uint value) => throw new NotSupportedException();
        protected override string Format(long value) => throw new NotSupportedException();
        protected override string Format(ulong value) => throw new NotSupportedException();
        protected override string Format(float value) => throw new NotSupportedException();
        protected override string Format(double value) => throw new NotSupportedException();
        protected override string FormatCharacter(uint value, IrCharacterKind kind) => throw new NotSupportedException();
        protected override string FormatArray(IrArrayLiteralBase array) => throw new NotSupportedException();
        protected override string FormatRecord(IrRecordLiteral record) => throw new NotSupportedException();
    }
}