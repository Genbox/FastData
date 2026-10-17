using System.Runtime.CompilerServices;

namespace Genbox.FastData.Ir.Internal;

/// <summary>Validates API arguments.</summary>
internal static class Validator
{
    /// <summary>Ensures an enum argument names a declared member.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the declared enum members.</exception>
    internal static void IsDefined<TEnum>(TEnum value, [CallerArgumentExpression(nameof(value))]string? paramName = null) where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
            throw new ArgumentOutOfRangeException(paramName, value, "The value must be a declared enum member.");
    }
}