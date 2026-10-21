using System.Globalization;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Ir.Emission;
using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.CSharp.Internal;

internal sealed class CSharpLiterals(IReadOnlyList<PreparedRecord> records, GeneratorEncoding encoding, Func<IrLiteral, string?>? reference = null) : LiteralFormatter(reference)
{
    private readonly CSharpText _text = new CSharpText(encoding);

    protected override string Signed(long value, IrTypeKind kind, bool element) => kind switch
    {
        IrTypeKind.I8 => element ? InvariantNumber(value) : $"((sbyte){InvariantNumber(value)})",
        IrTypeKind.I16 => element ? InvariantNumber(value) : $"((short){InvariantNumber(value)})",
        IrTypeKind.I32 => value == int.MinValue ? "int.MinValue" : InvariantNumber(value),
        IrTypeKind.I64 => value == long.MinValue ? "long.MinValue" : InvariantNumber(value) + "L",
        _ => throw new NotSupportedException($"Unsupported signed integer type '{kind}'.")
    };

    protected override string Unsigned(ulong value, IrTypeKind kind, bool element) => kind switch
    {
        IrTypeKind.U8 => element ? InvariantNumber(value) : $"((byte){InvariantNumber(value)})",
        IrTypeKind.U16 => element ? InvariantNumber(value) : $"((ushort){InvariantNumber(value)})",
        IrTypeKind.U32 => InvariantNumber(value) + "U",
        IrTypeKind.U64 => InvariantNumber(value) + "UL",
        _ => throw new NotSupportedException($"Unsupported unsigned integer type '{kind}'.")
    };

    // Infinities and the canonical NaN use the named constants so they stay valid in const declarations; other NaN payloads keep their bits.
    protected override string Format(float value)
    {
        if (float.IsPositiveInfinity(value))
            return "float.PositiveInfinity";

        if (float.IsNegativeInfinity(value))
            return "float.NegativeInfinity";

        if (float.IsNaN(value))
            return IsCanonicalNaN(value) ? "float.NaN" : $"BitConverter.Int32BitsToSingle({InvariantNumber(BitConverter.SingleToInt32Bits(value))})";

        return value.ToString("R", CultureInfo.InvariantCulture) + "f";
    }

    protected override string Format(double value)
    {
        if (double.IsPositiveInfinity(value))
            return "double.PositiveInfinity";

        if (double.IsNegativeInfinity(value))
            return "double.NegativeInfinity";

        if (double.IsNaN(value))
            return IsCanonicalNaN(value) ? "double.NaN" : $"BitConverter.Int64BitsToDouble({InvariantNumber(BitConverter.DoubleToInt64Bits(value))}L)";

        return value.ToString("R", CultureInfo.InvariantCulture) + "d";
    }

    /// <summary>Returns whether the literal prints as a C# constant expression.</summary>
    public static bool IsConstant(IrLiteral literal, GeneratorEncoding encoding) => literal switch
    {
        IrF32Literal x => !float.IsNaN(x.Value) || IsCanonicalNaN(x.Value),
        IrF64Literal x => !double.IsNaN(x.Value) || IsCanonicalNaN(x.Value),
        IrStringLiteral => encoding == GeneratorEncoding.Utf16CodeUnits,
        IrArrayLiteralBase or IrRecordLiteral => false,
        _ => true
    };

    protected override string FormatCharacter(uint value, IrCharacterKind kind) => CSharpText.Character(value);
    protected override string FormatString(ReadOnlySpan<char> value) => _text.Literal(value);
    protected override string FormatNull(IrType type) => "null";

    protected override string Aggregate(IrLiteral literal) => literal switch
    {
        IrArrayLiteralBase array => $"new {Type(array.ElementType)}[] {{ {string.Join(", ", Elements(array))} }}",
        IrRecordLiteral record => $"new {records[record.RecordId.Value].Name}({string.Join(", ", record.Fields.Select(Element))})",
        _ => throw new NotSupportedException($"Unsupported aggregate '{literal.GetType().Name}'.")
    };

    public override string Type(IrType type) => type.Kind switch
    {
        IrTypeKind.Bool => "bool",
        IrTypeKind.I8 => "sbyte",
        IrTypeKind.U8 => "byte",
        IrTypeKind.I16 => "short",
        IrTypeKind.U16 => "ushort",
        IrTypeKind.I32 => "int",
        IrTypeKind.U32 => "uint",
        IrTypeKind.I64 => "long",
        IrTypeKind.U64 => "ulong",
        IrTypeKind.F32 => "float",
        IrTypeKind.F64 => "double",
        IrTypeKind.String => _text.Type,
        IrTypeKind.Char => "char",
        IrTypeKind.Array => Type(type.ElementType!) + "[]",
        IrTypeKind.Record => records[type.RecordId!.Value.Value].Name + "?",
        _ => throw new NotSupportedException($"Type '{type}' is not supported by the C# emitter.")
    };

    private static bool IsCanonicalNaN(float value) => BitConverter.SingleToInt32Bits(value) == BitConverter.SingleToInt32Bits(float.NaN);
    private static bool IsCanonicalNaN(double value) => BitConverter.DoubleToInt64Bits(value) == BitConverter.DoubleToInt64Bits(double.NaN);
}