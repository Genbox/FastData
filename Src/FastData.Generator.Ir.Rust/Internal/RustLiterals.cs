using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Ir.Emission;
using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.Rust.Internal;

internal sealed class RustLiterals(IReadOnlyList<PreparedRecord> records, GeneratorEncoding encoding, string recordPrefix = "", Func<IrLiteral, string?>? reference = null) : LiteralFormatter(reference)
{
    private readonly RustText _text = new RustText(encoding);
    private readonly RustTypes _types = new RustTypes(records, encoding, recordPrefix);

    public override string Type(IrType type) => _types.Type(type);

    protected override string Signed(long value, IrTypeKind kind, bool element) => Number(value, RustTypes.ScalarName(kind));
    protected override string Unsigned(ulong value, IrTypeKind kind, bool element) => Number(value, RustTypes.ScalarName(kind));

    protected override string Format(float value) => $"f32::from_bits({Number(BitConverter.SingleToUInt32Bits(value), "u32")})";
    protected override string Format(double value) => $"f64::from_bits({Number(BitConverter.DoubleToUInt64Bits(value), "u64")})";
    protected override string FormatCharacter(uint value, IrCharacterKind kind) => Number(value, kind == IrCharacterKind.Ascii ? "u8" : "u16");
    protected override string FormatString(ReadOnlySpan<char> value) => _text.Literal(value);

    protected override string FormatNull(IrType type) => $"None::<{_types.ReferenceType(type)}>";

    protected override string Aggregate(IrLiteral literal) => literal switch
    {
        IrArrayLiteralBase array => $"Some(&[{string.Join(", ", Elements(array))}])",
        IrRecordLiteral record => Record(record),
        _ => throw new NotSupportedException($"Unsupported aggregate '{literal.GetType().Name}'.")
    };

    private string Record(IrRecordLiteral record)
    {
        PreparedRecord definition = records[record.RecordId.Value];
        string fields = string.Join(", ", record.Fields.Select((field, index) => $"{definition.FieldNames[index]}: {Literal(field)}"));
        return $"Some(&{_types.RecordName(record.RecordId.Value)} {{ {fields}, marker: core::marker::PhantomData }})";
    }

    private static string Number<T>(T value, string suffix) where T : IFormattable => InvariantNumber(value) + suffix;
}