namespace Genbox.FastData.Generators;

/// <summary>Identifies helper functions required by generated expressions.</summary>
[Flags]
public enum GeneratorFunction
{
    /// <summary>No helper functions are required.</summary>
    None = 0,

    /// <summary>Read one encoded unit at an offset.</summary>
    UnitAt = 1 << 1,

    /// <summary>Read one encoded unit and normalize ASCII letters to lowercase.</summary>
    UnitAtAsciiLower = 1 << 2,

    /// <summary>Read the input length.</summary>
    Length = 1 << 3,

    /// <summary>Compare a fragment at an offset.</summary>
    EqualsAt = 1 << 4,

    /// <summary>Compare a fragment at an offset using ASCII case folding.</summary>
    EqualsAtAsciiLower = 1 << 5,

    /// <summary>Read an unsigned 8-bit value.</summary>
    ReadU8 = 1 << 6,

    /// <summary>Read an unsigned 16-bit value.</summary>
    ReadU16 = 1 << 7,

    /// <summary>Read an unsigned 32-bit value.</summary>
    ReadU32 = 1 << 8,

    /// <summary>Read an unsigned 64-bit value.</summary>
    ReadU64 = 1 << 9,

    /// <summary>Test whether all input units are ASCII.</summary>
    IsAsciiOnly = 1 << 10
}