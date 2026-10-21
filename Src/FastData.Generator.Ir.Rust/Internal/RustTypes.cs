using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Ir.Prepared;
using Genbox.FastData.Ir.Model;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.Rust.Internal;

internal sealed class RustTypes(IReadOnlyList<PreparedRecord> records, GeneratorEncoding encoding, string recordPrefix)
{
    private readonly string _textType = new RustText(encoding).Type;

    public string Type(IrType type, string lifetime = "'static") => type.Kind switch
    {
        IrTypeKind.Char => type.CharacterKind == IrCharacterKind.Ascii ? "u8" : "u16",
        IrTypeKind.String or IrTypeKind.Array or IrTypeKind.Record => $"Option<{ReferenceType(type, lifetime)}>",
        _ => ScalarName(type.Kind)
    };

    public string RecordName(int index) => recordPrefix + records[index].Name;

    public string ReferenceType(IrType type, string lifetime = "'static") => type.Kind switch
    {
        IrTypeKind.String => $"&{lifetime} {_textType}",
        IrTypeKind.Array => $"&{lifetime} [{Type(type.ElementType!, lifetime)}]",
        IrTypeKind.Record => $"&{lifetime} {RecordName(type.RecordId!.Value.Value)}<{lifetime}>",
        _ => throw new NotSupportedException($"Unsupported Rust reference type '{type}'.")
    };

    public static string ScalarName(IrTypeKind kind) => kind switch
    {
        IrTypeKind.Bool => "bool",
        IrTypeKind.I8 => "i8",
        IrTypeKind.U8 => "u8",
        IrTypeKind.I16 => "i16",
        IrTypeKind.U16 => "u16",
        IrTypeKind.I32 => "i32",
        IrTypeKind.U32 => "u32",
        IrTypeKind.I64 => "i64",
        IrTypeKind.U64 => "u64",
        IrTypeKind.F32 => "f32",
        IrTypeKind.F64 => "f64",
        _ => throw new NotSupportedException($"Unsupported Rust scalar type '{kind}'.")
    };
}