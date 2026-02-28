using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Enums;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Framework;

internal class CPlusPlusConstantsDef : IConstantsDef
{
    public string Comment => "//";
    public Func<string, string, string> MinLengthTemplate => (type, value) => $"    static constexpr {type} min_key_length = {value};";
    public Func<string, string, string> MaxLengthTemplate => (type, value) => $"    static constexpr {type} max_key_length = {value};";
    public Func<string, string, string> MinValueTemplate => (type, value) => $"    static constexpr {type} min_key = {value};";
    public Func<string, string, string> MaxValueTemplate => (type, value) => $"    static constexpr {type} max_key = {value};";
    public Func<string, string, string> ItemCountTemplate => (type, value) => $"    static constexpr {type} item_count = {value};";

    public Func<CharacterClass, string> CharacterClassesTemplate => value =>
        $"""
             static constexpr bool has_number = {(value.HasFlag(CharacterClass.Number) ? "true" : "false")};
             static constexpr bool has_uppercase = {(value.HasFlag(CharacterClass.Uppercase) ? "true" : "false")};
             static constexpr bool has_lowercase = {(value.HasFlag(CharacterClass.Lowercase) ? "true" : "false")};
             static constexpr bool has_symbol = {(value.HasFlag(CharacterClass.Symbol) ? "true" : "false")};
             static constexpr bool has_whitespace = {(value.HasFlag(CharacterClass.Whitespace) ? "true" : "false")};
         """;
}