using System.Diagnostics.CodeAnalysis;

namespace Genbox.FastData.Generators.Enums;

/// <summary>Identifies character categories present in analyzed string keys.</summary>
[SuppressMessage("Naming", "CA1008:Enums should have zero value", Justification = "Unknown describes the absence of a recognized character class.")]
[Flags]
public enum CharacterClass : byte
{
    /// <summary>No recognized character category.</summary>
    Unknown = 0,

    /// <summary>Decimal digits.</summary>
    Number = 1,

    /// <summary>Uppercase letters.</summary>
    Uppercase = 2,

    /// <summary>Lowercase letters.</summary>
    Lowercase = 4,

    /// <summary>Symbols and punctuation.</summary>
    Symbol = 8,

    /// <summary>Whitespace characters.</summary>
    Whitespace = 16
}