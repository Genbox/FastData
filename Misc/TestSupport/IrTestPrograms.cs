using System.Globalization;
using Genbox.FastData.Ir.Execution;
using Genbox.FastData.Ir.Interop;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;
using static Genbox.FastData.Ir.Building.IrFactory;

namespace Genbox.FastData.Ir.TestSupport;

internal static class IrTestPrograms
{
    public static IrLiteral Constant(object value) => IrClrConverter.FromValue(value);

    public static IrLiteral Evaluate(IrType type, IrExpression expression, IrTextEncoding encoding = IrTextEncoding.Utf16, IReadOnlyList<IrRecord>? records = null)
    {
        IrFunction function = new IrFunction("Evaluate", type, [], [], Block(Return(expression)));
        IrProgram program = new IrProgram([function], records: records, textEncoding: encoding);
        return new IrInterpreter(program).Invoke(new IrFunctionId(0));
    }

    public static IrProgram WithLiteral(IrLiteral literal) => new IrProgram([], [new IrData("value", literal)]);

    public static IrIfStatement ReturnIf(IrExpression condition, IrExpression ifTrue, IrExpression ifFalse) => new IrIfStatement(condition, Block(Return(ifTrue)), Block(Return(ifFalse)));

    // Compact textual conformance vectors define expected literal values for tests.
    public static IrLiteral VectorLiteral(IrType type, string text) => type.Kind switch
    {
        IrTypeKind.Bool => new IrBoolLiteral(bool.Parse(text)),
        IrTypeKind.Char => new IrCharLiteral(type.CharacterKind!.Value, uint.Parse(text, CultureInfo.InvariantCulture)),
        IrTypeKind.I8 => new IrI8Literal(sbyte.Parse(text, CultureInfo.InvariantCulture)),
        IrTypeKind.U8 => new IrU8Literal(byte.Parse(text, CultureInfo.InvariantCulture)),
        IrTypeKind.I16 => new IrI16Literal(short.Parse(text, CultureInfo.InvariantCulture)),
        IrTypeKind.U16 => new IrU16Literal(ushort.Parse(text, CultureInfo.InvariantCulture)),
        IrTypeKind.I32 => new IrI32Literal(int.Parse(text, CultureInfo.InvariantCulture)),
        IrTypeKind.U32 => new IrU32Literal(uint.Parse(text, CultureInfo.InvariantCulture)),
        IrTypeKind.I64 => new IrI64Literal(long.Parse(text, CultureInfo.InvariantCulture)),
        IrTypeKind.U64 => new IrU64Literal(ulong.Parse(text, CultureInfo.InvariantCulture)),
        IrTypeKind.F32 => new IrF32Literal(uint.Parse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture)),
        IrTypeKind.F64 => new IrF64Literal(ulong.Parse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture)),
        IrTypeKind.String => new IrStringLiteral(text),
        _ => throw new NotSupportedException()
    };
}