using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Ir.Emission;
using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.CPlusPlus.Internal;

internal sealed class CPlusPlusLiterals(IReadOnlyList<PreparedRecord> records, GeneratorEncoding encoding, Func<IrLiteral, string?>? reference = null) : LiteralFormatter(reference)
{
    private readonly CPlusPlusText _text = new CPlusPlusText(encoding);

    public override string Type(IrType type) => type.Kind switch
    {
        IrTypeKind.Bool => "bool",
        IrTypeKind.I8 => "std::int8_t",
        IrTypeKind.U8 => "std::uint8_t",
        IrTypeKind.I16 => "std::int16_t",
        IrTypeKind.U16 => "std::uint16_t",
        IrTypeKind.I32 => "std::int32_t",
        IrTypeKind.U32 => "std::uint32_t",
        IrTypeKind.I64 => "std::int64_t",
        IrTypeKind.U64 => "std::uint64_t",
        IrTypeKind.F32 => "float",
        IrTypeKind.F64 => "double",
        IrTypeKind.Char => type.CharacterKind == IrCharacterKind.Ascii ? "std::uint8_t" : "std::uint16_t",
        IrTypeKind.String => $"std::optional<{_text.Type}>",
        IrTypeKind.Array => $"std::optional<std::span<{Type(type.ElementType!)} const>>",
        IrTypeKind.Record => $"const {records[type.RecordId!.Value.Value].Name}*",
        _ => throw new NotSupportedException($"Unsupported C++ type '{type}'.")
    };

    protected override string Signed(long value, IrTypeKind kind, bool element) => kind switch
    {
        IrTypeKind.I8 => Number(value, "std::int8_t"),
        IrTypeKind.I16 => Number(value, "std::int16_t"),
        IrTypeKind.I32 => value == int.MinValue ? "(-2147483647 - 1)" : Number(value, "std::int32_t"),
        IrTypeKind.I64 => value == long.MinValue ? "(-9223372036854775807LL - 1)" : InvariantNumber(value) + "LL",
        _ => throw new NotSupportedException($"Unsupported signed integer type '{kind}'.")
    };

    protected override string Unsigned(ulong value, IrTypeKind kind, bool element) => kind switch
    {
        IrTypeKind.U8 => Number(value, "std::uint8_t"),
        IrTypeKind.U16 => Number(value, "std::uint16_t"),
        IrTypeKind.U32 => InvariantNumber(value) + "U",
        IrTypeKind.U64 => InvariantNumber(value) + "ULL",
        _ => throw new NotSupportedException($"Unsupported unsigned integer type '{kind}'.")
    };

    protected override string Format(float value) => $"std::bit_cast<float>({Number(BitConverter.SingleToUInt32Bits(value), "std::uint32_t")})";
    protected override string Format(double value) => $"std::bit_cast<double>({Unsigned(BitConverter.DoubleToUInt64Bits(value), IrTypeKind.U64, false)})";
    protected override string FormatCharacter(uint value, IrCharacterKind kind) => Number(value, kind == IrCharacterKind.Ascii ? "std::uint8_t" : "std::uint16_t");
    protected override string FormatNull(IrType type) => type.Kind == IrTypeKind.Record ? "nullptr" : $"{Type(type)}{{}}";

    protected override string FormatString(ReadOnlySpan<char> value) => _text.Literal(value);

    protected override string Aggregate(IrLiteral literal) => literal switch
    {
        IrArrayLiteralBase array => $"{{ {string.Join(", ", Elements(array))} }}",
        IrRecordLiteral record => $"{{ {string.Join(", ", record.Fields.Select(Element))} }}",
        _ => throw new NotSupportedException($"Unsupported aggregate '{literal.GetType().Name}'.")
    };

    private static string Number<T>(T value, string type) where T : IFormattable => $"static_cast<{type}>({InvariantNumber(value)})";
}