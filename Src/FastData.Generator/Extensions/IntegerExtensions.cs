using System.Globalization;
using JetBrains.Annotations;

namespace Genbox.FastData.Generator.Extensions;

/// <summary>Provides culture-invariant formatting for integral values.</summary>
[PublicAPI]
public static class IntegerExtensions
{
    /// <summary>Formats a signed 16-bit value using the invariant culture.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The invariant string representation.</returns>
    public static string ToStringInvariant(this short value) => value.ToString(NumberFormatInfo.InvariantInfo);

    /// <summary>Formats an unsigned 16-bit value using the invariant culture.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The invariant string representation.</returns>
    public static string ToStringInvariant(this ushort value) => value.ToString(NumberFormatInfo.InvariantInfo);

    /// <summary>Formats a signed 32-bit value using the invariant culture.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The invariant string representation.</returns>
    public static string ToStringInvariant(this int value) => value.ToString(NumberFormatInfo.InvariantInfo);

    /// <summary>Formats an unsigned 32-bit value using the invariant culture.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The invariant string representation.</returns>
    public static string ToStringInvariant(this uint value) => value.ToString(NumberFormatInfo.InvariantInfo);

    /// <summary>Formats an unsigned 64-bit value using the invariant culture.</summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The invariant string representation.</returns>
    public static string ToStringInvariant(this ulong value) => value.ToString(NumberFormatInfo.InvariantInfo);
}