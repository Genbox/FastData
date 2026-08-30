namespace Genbox.FastData.Enums;

/// <summary>Specifies the unit used to represent encoded string lengths and positions.</summary>
public enum GeneratorEncoding : byte
{
    /// <summary>No encoding has been selected.</summary>
    Unknown = 0,

    // Number of bytes in a specific encoding
    /// <summary>Represents ASCII-encoded byte positions and lengths.</summary>
    AsciiBytes,

    /// <summary>Represents UTF-8-encoded byte positions and lengths.</summary>
    Utf8Bytes,

    /// <summary>Represents UTF-16-encoded byte positions and lengths.</summary>
    Utf16Bytes,

    // Number of code units in a specific encoding / runtime model
    /// <summary>Represents UTF-16 code-unit positions and lengths.</summary>
    Utf16CodeUnits
}