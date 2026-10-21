using System.Globalization;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Generator.Ir.Emission;
using Genbox.FastData.Generator.Ir.Operations;
using Genbox.FastData.Ir.Model.Enums;

namespace Genbox.FastData.Generator.Ir.CPlusPlus;

/// <summary>Formats C++ text views, literals, comparisons, and runtime operations.</summary>
public sealed class CPlusPlusText(GeneratorEncoding encoding)
{
    /// <summary>The public text view type; stored text wraps this in std::optional.</summary>
    public string Type { get; } = encoding switch
    {
        GeneratorEncoding.AsciiBytes => "std::span<const std::uint8_t>",
        GeneratorEncoding.Utf8Bytes => "std::u8string_view",
        GeneratorEncoding.Utf16CodeUnits => "std::u16string_view",
        _ => throw new NotSupportedException($"Unsupported C++ text encoding '{encoding}'.")
    };

    /// <summary>Formats comparison of optional text views by content, ordering null before non-null.</summary>
    public string Comparison(BinaryOperation operation, string left, string right)
    {
        string symbol = CStyleOperators.Symbol(operation);
        if (encoding != GeneratorEncoding.AsciiBytes)
            return $"({left} {symbol} {right})";

        // std::span has no content comparison operators. Keep the comparator self-contained for companion scopes.
        return $$"""
                 ([](auto left, auto right) {
                     if (!left || !right)
                         return static_cast<bool>(left) <=> static_cast<bool>(right);

                     return std::lexicographical_compare_three_way(left->begin(), left->end(), right->begin(), right->end());
                 }({{left}}, {{right}}) {{symbol}} 0)
                 """;
    }

    /// <summary>Formats a string literal with storage that outlives its view.</summary>
    internal string Literal(ReadOnlySpan<char> value)
    {
        if (encoding == GeneratorEncoding.Utf16CodeUnits)
        {
            StringBuilder units = new StringBuilder(value.Length * 6);
            foreach (char unit in value)
                units.Append("\\x").Append(((ushort)unit).ToString("X4", CultureInfo.InvariantCulture));

            return $"std::optional<{Type}>{{{Type}{{u\"{units}\", {value.Length}}}}}";
        }

        byte[] bytes = EncodeBytes(value, encoding);

        if (encoding == GeneratorEncoding.AsciiBytes)
        {
            string elements = string.Join(", ", bytes.Select(InvariantNumber));
            return $$"""
                     []() {
                         static constexpr std::array<std::uint8_t, {{bytes.Length}}> bytes = { {{elements}} };
                         return std::optional<{{Type}}>{ {{Type}}{bytes} };
                     }()
                     """;
        }

        StringBuilder text = new StringBuilder(bytes.Length * 4);
        foreach (byte item in bytes)
            text.Append("\\x").Append(item.ToString("X2", CultureInfo.InvariantCulture));

        return $"std::optional<{Type}>{{{Type}{{u8\"{text}\", {bytes.Length}}}}}";
    }

    /// <summary>Formats native text length and code-unit access.</summary>
    internal string Call(IrBuiltinOperation operation, string[] arguments) => operation switch
    {
        IrBuiltinOperation.TextLength => $"static_cast<std::int32_t>(({arguments[0]})->size())",
        IrBuiltinOperation.TextUnitAt => $"static_cast<{(encoding == GeneratorEncoding.Utf16CodeUnits ? "std::uint16_t" : "std::uint8_t")}>((*({arguments[0]}))[{arguments[1]}])",
        _ => throw new NotSupportedException($"Unsupported C++ builtin '{operation}'.")
    };
}