using Genbox.FastData.Ir.Building;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using static Genbox.FastData.Ir.TestSupport.IrTestPrograms;
using static Genbox.FastData.Ir.Building.IrFactory;

namespace Genbox.FastData.Ir.Tests;

public class IrComparisonTests
{
    [Theory]
    [InlineData(BinaryOperation.Equal, 7, 7, true)]
    [InlineData(BinaryOperation.NotEqual, 7, 7, false)]
    [InlineData(BinaryOperation.LessThan, -1, 1, true)]
    [InlineData(BinaryOperation.LessThanOrEqual, 7, 7, true)]
    [InlineData(BinaryOperation.GreaterThan, 7, 7, false)]
    [InlineData(BinaryOperation.GreaterThanOrEqual, 8, 7, true)]
    [InlineData(BinaryOperation.Equal, double.NaN, double.NaN, false)]
    [InlineData(BinaryOperation.NotEqual, double.NaN, 1d, true)]
    [InlineData(BinaryOperation.LessThan, double.NaN, 1d, false)]
    [InlineData(BinaryOperation.Equal, -0.0d, 0d, true)]
    public void ScalarComparisonsUseIntegerAndIeeeOrder(BinaryOperation operation, object left, object right, bool expected)
    {
        IrExpression comparison = Binary(operation, Constant(left), Constant(right));
        Assert.Equal(Constant(expected), Evaluate(IrType.Bool, comparison));
    }

    [Theory]
    [InlineData(IrTextEncoding.Utf8, "same", "same", 0)]
    [InlineData(IrTextEncoding.Utf16, "a", "A", 1)]
    [InlineData(IrTextEncoding.Utf8, "a", "ab", -1)]
    [InlineData(IrTextEncoding.Utf16, "a\0", "a", 1)]
    [InlineData(IrTextEncoding.Utf8, "é", "e\u0301", 1)]
    [InlineData(IrTextEncoding.Utf8, "\uE000", "😀", -1)]
    [InlineData(IrTextEncoding.Utf16, "\uE000", "😀", 1)]
    [InlineData(IrTextEncoding.Utf8, "\uD7FF", "\U00010000", -1)]
    [InlineData(IrTextEncoding.Utf8, "\U00010000", "\uFFFF", 1)]
    [InlineData(IrTextEncoding.Utf8, "\U00010000", "\U00010001", -1)]
    [InlineData(IrTextEncoding.Utf8, "\U000103FF", "\U00010400", -1)]
    [InlineData(IrTextEncoding.Utf8, "a\U0010FFFF", "a\uFFFF", 1)]
    public void StringComparisonsUseEncodedUnitOrder(IrTextEncoding encoding, string left, string right, int expected)
    {
        Assert.Equal(Constant(expected == 0), Evaluate(IrType.Bool, Equal(Constant(left), Constant(right)), encoding));
        Assert.Equal(Constant(expected < 0), Evaluate(IrType.Bool, Less(Constant(left), Constant(right)), encoding));
    }

    [Theory]
    [InlineData("strings")]
    [InlineData("integers")]
    [InlineData("record")]
    public void AggregateNullChecksWorkInEitherOperandPosition(string representation)
    {
        IrRecord[] records = [new IrRecord("Entry", [])];
        IrLiteral value = representation switch
        {
            "strings" => new IrText(IrTextEncoding.Utf16).PackStrings(["seven"]),
            "integers" => new IrPackedArrayLiteral<int>([7]),
            _ => new IrRecordLiteral(new IrRecordId(0), [])
        };

        IrNullLiteral nil = new IrNullLiteral(value.Type);
        IrLiteral Compare(BinaryOperation operation, IrExpression left, IrExpression right) => Evaluate(IrType.Bool, Binary(operation, left, right), records: records);

        Assert.Equal(Constant(false), Compare(BinaryOperation.Equal, value, nil));
        Assert.Equal(Constant(false), Compare(BinaryOperation.Equal, nil, value));
        Assert.Equal(Constant(true), Compare(BinaryOperation.Equal, nil, nil));
        Assert.Equal(Constant(true), Compare(BinaryOperation.NotEqual, nil, value));
        Assert.Equal(Constant(true), Compare(BinaryOperation.NotEqual, value, nil));
        Assert.Equal(Constant(false), Compare(BinaryOperation.NotEqual, nil, nil));
    }
}