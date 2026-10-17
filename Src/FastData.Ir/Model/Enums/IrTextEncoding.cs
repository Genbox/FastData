namespace Genbox.FastData.Ir.Model.Enums;

/// <summary>The encoding used for all String values in a program.</summary>
public enum IrTextEncoding
{
    Unknown = 0,

    /// <summary>Well-formed UTF-8 stored in U8 code units.</summary>
    Utf8,

    /// <summary>UTF-16 stored in U16 code units, including unpaired surrogates.</summary>
    Utf16
}