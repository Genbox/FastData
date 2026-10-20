using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Ir.Model;

/// <summary>A target-independent type: a scalar, a character, an array, or a nominal record.</summary>
/// <remarks>
/// Static members and factories construct valid type combinations.
/// Equality compares the kind, character kind, array element type, and nominal record ID.
/// </remarks>
public sealed record IrType
{
    private static readonly IrType _ascii = new IrType(IrTypeKind.Char, characterKind: IrCharacterKind.Ascii);
    private static readonly IrType _utf16 = new IrType(IrTypeKind.Char, characterKind: IrCharacterKind.Utf16CodeUnit);

    private IrType(IrTypeKind kind, IrType? elementType = null, IrRecordId? recordId = null, IrCharacterKind? characterKind = null)
    {
        Kind = kind;
        ElementType = elementType;
        RecordId = recordId;
        CharacterKind = characterKind;
    }

    public IrTypeKind Kind { get; }

    /// <summary>The element type of an array; null otherwise.</summary>
    public IrType? ElementType { get; }

    /// <summary>The declaring record of a record type; null otherwise.</summary>
    public IrRecordId? RecordId { get; }

    /// <summary>The character kind of a character type; null otherwise.</summary>
    public IrCharacterKind? CharacterKind { get; }

    public bool IsSignedIntegral => Kind is IrTypeKind.I8 or IrTypeKind.I16 or IrTypeKind.I32 or IrTypeKind.I64;

    public bool IsUnsignedIntegral => Kind is IrTypeKind.U8 or IrTypeKind.U16 or IrTypeKind.U32 or IrTypeKind.U64;

    public bool IsIntegral => IsSignedIntegral || IsUnsignedIntegral;

    public bool IsFloatingPoint => Kind is IrTypeKind.F32 or IrTypeKind.F64;

    public bool IsNumeric => IsIntegral || IsFloatingPoint;

    public bool AcceptsNull => Kind is IrTypeKind.String or IrTypeKind.Array or IrTypeKind.Record;

    public int BitWidth => Kind switch
    {
        IrTypeKind.I8 or IrTypeKind.U8 => 8,
        IrTypeKind.I16 or IrTypeKind.U16 => 16,
        IrTypeKind.I32 or IrTypeKind.U32 or IrTypeKind.F32 => 32,
        IrTypeKind.I64 or IrTypeKind.U64 or IrTypeKind.F64 => 64,
        _ => 0
    };

    public static IrType Bool { get; } = new IrType(IrTypeKind.Bool);
    public static IrType I8 { get; } = new IrType(IrTypeKind.I8);
    public static IrType U8 { get; } = new IrType(IrTypeKind.U8);
    public static IrType I16 { get; } = new IrType(IrTypeKind.I16);
    public static IrType U16 { get; } = new IrType(IrTypeKind.U16);
    public static IrType I32 { get; } = new IrType(IrTypeKind.I32);
    public static IrType U32 { get; } = new IrType(IrTypeKind.U32);
    public static IrType I64 { get; } = new IrType(IrTypeKind.I64);
    public static IrType U64 { get; } = new IrType(IrTypeKind.U64);
    public static IrType F32 { get; } = new IrType(IrTypeKind.F32);
    public static IrType F64 { get; } = new IrType(IrTypeKind.F64);
    public static IrType String { get; } = new IrType(IrTypeKind.String);

    /// <summary>A scalar type: Bool, a numeric kind, or String.</summary>
    public static IrType Scalar(IrTypeKind kind) => kind switch
    {
        IrTypeKind.Bool => Bool,
        IrTypeKind.I8 => I8,
        IrTypeKind.U8 => U8,
        IrTypeKind.I16 => I16,
        IrTypeKind.U16 => U16,
        IrTypeKind.I32 => I32,
        IrTypeKind.U32 => U32,
        IrTypeKind.I64 => I64,
        IrTypeKind.U64 => U64,
        IrTypeKind.F32 => F32,
        IrTypeKind.F64 => F64,
        IrTypeKind.String => String,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), $"'{kind}' types are built with Char, ArrayOf, or Record.")
    };

    /// <summary>A character of a fixed character kind. Characters compare by unit value and convert only to and from <see cref="UnitOf" />.</summary>
    public static IrType Char(IrCharacterKind characterKind) => characterKind switch
    {
        IrCharacterKind.Ascii => _ascii,
        IrCharacterKind.Utf16CodeUnit => _utf16,
        _ => throw new ArgumentOutOfRangeException(nameof(characterKind))
    };

    /// <summary>The unsigned integer type that holds a character kind's values: U8 for Ascii, U16 for Utf16CodeUnit.</summary>
    public static IrType UnitOf(IrCharacterKind characterKind) => characterKind switch
    {
        IrCharacterKind.Ascii => U8,
        IrCharacterKind.Utf16CodeUnit => U16,
        _ => throw new ArgumentOutOfRangeException(nameof(characterKind))
    };

    /// <summary>The unsigned integral type of the same width as an integral type: U8 for I8 and U8, U16 for I16 and U16, and so on.</summary>
    public static IrType UnsignedOf(IrType type) => type.Kind switch
    {
        IrTypeKind.I8 or IrTypeKind.U8 => U8,
        IrTypeKind.I16 or IrTypeKind.U16 => U16,
        IrTypeKind.I32 or IrTypeKind.U32 => U32,
        IrTypeKind.I64 or IrTypeKind.U64 => U64,
        _ => throw new ArgumentException($"'{type}' is not an integral type.", nameof(type))
    };

    public static IrType ArrayOf(IrType elementType) => new IrType(IrTypeKind.Array, elementType ?? throw Fail("An array element type is required."));

    public static IrType Record(IrRecordId id) => new IrType(IrTypeKind.Record, recordId: id);

    public override string ToString() => Kind switch
    {
        IrTypeKind.Array => $"Array<{ElementType}>",
        IrTypeKind.Record => $"Record#{RecordId!.Value.Value}",
        IrTypeKind.Char => $"Char<{CharacterKind}>",
        _ => Kind.ToString()
    };
}