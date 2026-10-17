namespace Genbox.FastData.Ir.Model.Enums;

/// <summary>Explicit numeric conversion policies.</summary>
public enum ConversionKind
{
    Unknown = 0,

    /// <summary>
    /// Integral-to-integral conversion is modulo 2^W at target width W: signed sources sign-extend and unsigned sources zero-extend, so widening preserves the value.
    /// Character conversions use their unit type; producers must ensure the value fits the character kind (0–127 for ASCII, 0–65535 for UTF-16 code units).
    /// </summary>
    Numeric,

    /// <summary>Reinterprets the bits of a binary floating-point value as an equal-width integer, or the reverse, preserving the exact bits, including signed zero and NaN payloads.</summary>
    BitCast
}