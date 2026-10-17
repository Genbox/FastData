using Genbox.FastData.Ir.Model.Abstracts;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Ir.Model;

/// <summary>A character of a fixed character kind, stored as its unsigned unit value.</summary>
public sealed record IrCharLiteral(IrCharacterKind CharacterKind, uint Value) : IrLiteral
{
    public override IrType Type => IrType.Char(CharacterKind);

    /// <summary>Tests whether a unit value belongs to a character kind.</summary>
    public static bool IsValid(IrCharacterKind characterKind, uint value) => characterKind switch
    {
        IrCharacterKind.Ascii => value <= 0x7F,
        IrCharacterKind.Utf16CodeUnit => value <= 0xFFFF,
        _ => false
    };
}