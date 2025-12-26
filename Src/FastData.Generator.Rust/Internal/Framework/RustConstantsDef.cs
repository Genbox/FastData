using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators;

namespace Genbox.FastData.Generator.Rust.Internal.Framework;

internal class RustConstantsDef : IConstantsDef
{
    public string Comment => "//!";
    public Func<string, string, string> MinLengthTemplate => (type, value) => $"    pub const MIN_KEY_LENGTH: {type} = {value};";
    public Func<string, string, string> MaxLengthTemplate => (type, value) => $"    pub const MAX_KEY_LENGTH: {type} = {value};";
    public Func<string, string, string> MinValueTemplate => (type, value) => $"    pub const MIN_KEY: {type} = {value};";
    public Func<string, string, string> MaxValueTemplate => (type, value) => $"    pub const MAX_KEY: {type} = {value};";
    public Func<string, string, string> ItemCountTemplate => (type, value) => $"    pub const ITEM_COUNT: {type} = {value};";

    public Func<CharacterClass, string> CharacterClassesTemplate => value =>
        $"""
             pub const HAS_NUMBER: bool = {(value.HasFlag(CharacterClass.Number) ? "true" : "false")};
             pub const HAS_UPPERCASE: bool = {(value.HasFlag(CharacterClass.Uppercase) ? "true" : "false")};
             pub const HAS_LOWERCASE: bool = {(value.HasFlag(CharacterClass.Lowercase) ? "true" : "false")};
             pub const HAS_SYMBOL: bool = {(value.HasFlag(CharacterClass.Symbol) ? "true" : "false")};
             pub const HAS_WHITESPACE: bool = {(value.HasFlag(CharacterClass.Whitespace) ? "true" : "false")};
         """;
}