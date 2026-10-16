using System.Text;
using Genbox.FastData.Enums;

namespace Genbox.FastData.Generators.Helpers;

/// <summary>Provides string operations for generator encodings.</summary>
public static class StringHelper
{
    internal static Encoding GetEncoding(GeneratorEncoding encoding) => encoding switch
    {
        GeneratorEncoding.AsciiBytes => Encoding.ASCII,
        GeneratorEncoding.Utf8Bytes => Encoding.UTF8,
        GeneratorEncoding.Utf16Bytes or GeneratorEncoding.Utf16CodeUnits => Encoding.Unicode,
        _ => throw new InvalidOperationException($"Unsupported encoding: {encoding}")
    };

    /// <summary>Returns a function that measures strings in the units of the specified encoding.</summary>
    public static Func<string, int> GetLengthFunc(GeneratorEncoding encoding)
    {
        if (encoding == GeneratorEncoding.Utf16CodeUnits)
            return static s => s.Length;

        Encoding enc = GetEncoding(encoding);
        return enc.GetByteCount;
    }

    internal static Func<string, byte[]> GetBytesFunc(GeneratorEncoding encoding)
    {
        Encoding enc = GetEncoding(encoding);
        return enc.GetBytes;
    }

    internal static int GetSize(GeneratorEncoding encoding) => encoding switch
    {
        GeneratorEncoding.AsciiBytes or GeneratorEncoding.Utf8Bytes => 1,
        GeneratorEncoding.Utf16Bytes or GeneratorEncoding.Utf16CodeUnits => 2,
        _ => throw new InvalidOperationException($"Unsupported encoding: {encoding}")
    };
}