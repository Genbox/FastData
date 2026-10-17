namespace Genbox.FastData.Ir.Model.Enums;

/// <summary>The value range of a <see cref="IrTypeKind.Char" /> type, independent of the program's String encoding.</summary>
public enum IrCharacterKind
{
    Unknown = 0,

    /// <summary>0x00 to 0x7F, stored as one byte.</summary>
    Ascii,

    /// <summary>Any UTF-16 code unit, 0x0000 to 0xFFFF, including unpaired surrogates.</summary>
    Utf16CodeUnit
}