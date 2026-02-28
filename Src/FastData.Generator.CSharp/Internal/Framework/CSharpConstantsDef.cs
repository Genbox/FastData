using Genbox.FastData.Generator.Framework.Interfaces;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Enums;

namespace Genbox.FastData.Generator.CSharp.Internal.Framework;

internal class CSharpConstantsDef : IConstantsDef
{
    public string Comment => "//";
    public Func<string, string, string> MinLengthTemplate => (type, value) => $"    public const {type} MinKeyLength = {value};";
    public Func<string, string, string> MaxLengthTemplate => (type, value) => $"    public const {type} MaxKeyLength = {value};";
    public Func<string, string, string> MinValueTemplate => (type, value) => $"    public const {type} MinKey = {value};";
    public Func<string, string, string> MaxValueTemplate => (type, value) => $"    public const {type} MaxKey = {value};";
    public Func<string, string, string> ItemCountTemplate => (type, value) => $"    public const {type} ItemCount = {value};";

    public Func<CharacterClass, string> CharacterClassesTemplate => value =>
        $"""
             public const bool HasNumber = {(value.HasFlag(CharacterClass.Number) ? "true" : "false")};
             public const bool HasUppercase = {(value.HasFlag(CharacterClass.Uppercase) ? "true" : "false")};
             public const bool HasLowercase = {(value.HasFlag(CharacterClass.Lowercase) ? "true" : "false")};
             public const bool HasSymbol = {(value.HasFlag(CharacterClass.Symbol) ? "true" : "false")};
             public const bool HasWhitespace = {(value.HasFlag(CharacterClass.Whitespace) ? "true" : "false")};
         """;
}