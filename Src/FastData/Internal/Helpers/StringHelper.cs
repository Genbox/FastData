using System.Text;
using Genbox.FastData.Enums;

namespace Genbox.FastData.Internal.Helpers;

internal static class StringHelper
{
    internal static Func<string, int> GetLengthFunc(GeneratorEncoding encoding) => encoding switch
    {
        GeneratorEncoding.AsciiBytes => static s => Encoding.ASCII.GetByteCount(s),
        GeneratorEncoding.Utf8Bytes => static s => Encoding.UTF8.GetByteCount(s),
        GeneratorEncoding.Utf16Bytes => static s => Encoding.Unicode.GetByteCount(s),
        GeneratorEncoding.Utf16CodeUnits => static s => s.Length,
        _ => throw new InvalidOperationException($"Unsupported length semantics: {encoding}")
    };

    internal static Func<string, byte[]> GetBytesFunc(GeneratorEncoding encoding) => encoding switch
    {
        GeneratorEncoding.AsciiBytes => static s => Encoding.ASCII.GetBytes(s),
        GeneratorEncoding.Utf8Bytes => static s => Encoding.UTF8.GetBytes(s),
        GeneratorEncoding.Utf16Bytes => static s => Encoding.Unicode.GetBytes(s),
        GeneratorEncoding.Utf16CodeUnits => static s => Encoding.Unicode.GetBytes(s),
        _ => throw new InvalidOperationException($"Unsupported encoding: {encoding}")
    };

    internal static int GetSize(GeneratorEncoding encoding) => encoding switch
    {
        GeneratorEncoding.AsciiBytes => 1,
        GeneratorEncoding.Utf8Bytes => 1,
        GeneratorEncoding.Utf16Bytes or GeneratorEncoding.Utf16CodeUnits => 2,
        _ => throw new InvalidOperationException($"Unsupported encoding: {encoding}")
    };

    internal static StringComparer GetStringComparer(bool ignoreCase) => ignoreCase switch
    {
        true => StringComparer.OrdinalIgnoreCase,
        false => StringComparer.Ordinal
    };
}