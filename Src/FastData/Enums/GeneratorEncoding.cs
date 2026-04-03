namespace Genbox.FastData.Enums;

public enum GeneratorEncoding : byte
{
    Unknown = 0,

    // Number of bytes in a specific encoding
    AsciiBytes,
    Utf8Bytes,
    Utf16Bytes,

    // Number of code units in a specific encoding / runtime model
    Utf16CodeUnits,
}