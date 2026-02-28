namespace Genbox.FastData.Generators.Enums;

[Flags]
public enum CharacterClass : byte
{
    Unknown = 0,
    Number = 1,
    Uppercase = 2,
    Lowercase = 4,
    Symbol = 8,
    Whitespace = 16
}