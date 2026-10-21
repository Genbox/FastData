using System.Globalization;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Ir.Operations;

namespace Genbox.FastData.Generator.Ir.Rust.Internal;

/// <summary>Formats Rust text types, literals, comparisons, and runtime operations.</summary>
internal sealed class RustText(GeneratorEncoding encoding)
{
    public string Type { get; } = encoding switch
    {
        GeneratorEncoding.AsciiBytes => "[u8]",
        GeneratorEncoding.Utf8Bytes => "str",
        GeneratorEncoding.Utf16CodeUnits => "[u16]",
        _ => throw new NotSupportedException($"Unsupported Rust text encoding '{encoding}'.")
    };

    private string UnitsSuffix { get; } = encoding == GeneratorEncoding.Utf8Bytes ? ".as_bytes()" : "";

    public static IrBuiltinOperation SupportedBuiltins(GeneratorEncoding encoding) => encoding == GeneratorEncoding.Utf16CodeUnits
        ? IrBuiltins.All & ~IrBuiltins.ByteReads
        : IrBuiltins.All;

    public string Call(IrBuiltinOperation operation, string[] arguments)
    {
        string units = $"{arguments[0]}.unwrap()" + UnitsSuffix;
        return operation switch
        {
            IrBuiltinOperation.TextLength => $"({arguments[0]}.unwrap().len() as i32)",
            IrBuiltinOperation.TextUnitAt => $"{units}[({arguments[1]}) as usize]",
            IrBuiltinOperation.TextIsAscii => encoding != GeneratorEncoding.Utf16CodeUnits ? $"{arguments[0]}.unwrap().is_ascii()" : $"{units}.iter().all(|unit| *unit < 128)",
            IrBuiltinOperation.TextEqualsAt => $$"""
                                                 {
                                                     let (text, offset, fragment) = ({{units}}, ({{arguments[1]}}) as usize, ({{arguments[2]}}).unwrap(){{UnitsSuffix}});
                                                     &text[offset..offset + fragment.len()] == fragment
                                                 }
                                                 """,
            IrBuiltinOperation.ReadU8 => $"({units}[({arguments[1]}) as usize] as u32)",
            IrBuiltinOperation.ReadU16LittleEndian => Read(arguments, "u16", "u32"),
            IrBuiltinOperation.ReadU32LittleEndian => Read(arguments, "u32", "u32"),
            IrBuiltinOperation.ReadU64LittleEndian => Read(arguments, "u64", "u64"),
            _ => throw new NotSupportedException($"Unsupported Rust builtin '{operation}'.")
        };
    }

    // Builtin preconditions guarantee the entire unaligned read fits within the UTF-8 bytes.
    private static string Read(string[] arguments, string readType, string resultType)
    {
        string pointer = $"{arguments[0]}.unwrap().as_ptr().add(({arguments[1]}) as usize) as *const {readType}";
        return $"(unsafe {{ {readType}::from_le(core::ptr::read_unaligned({pointer})) }} as {resultType})";
    }

    public string Literal(ReadOnlySpan<char> value)
    {
        if (encoding == GeneratorEncoding.Utf8Bytes)
            return $"Some({Quote(value, '"', static (x, x2) => x.Append(@"\u{").Append(x2.ToString("x", CultureInfo.InvariantCulture)).Append('}'), true)})";

        StringBuilder text = new StringBuilder("Some(&[");

        for (int i = 0; i < value.Length; i++)
        {
            if (i != 0)
                text.Append(", ");

            if (encoding == GeneratorEncoding.AsciiBytes && value[i] > 127)
                throw new NotSupportedException("ASCII text cannot contain non-ASCII characters.");

            text.Append(InvariantNumber((ushort)value[i]) + (encoding == GeneratorEncoding.AsciiBytes ? "u8" : "u16"));
        }

        return text.Append("])").ToString();
    }
}