using System.Globalization;

namespace Genbox.FastData.Generator.Extensions;

public static class IntegerExtensions
{
    public static string ToStringInvariant(this int value) => value.ToString(NumberFormatInfo.InvariantInfo);
    public static string ToStringInvariant(this uint value) => value.ToString(NumberFormatInfo.InvariantInfo);
    public static string ToStringInvariant(this ulong value) => value.ToString(NumberFormatInfo.InvariantInfo);
}