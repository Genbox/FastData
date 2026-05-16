using System.Globalization;
using JetBrains.Annotations;

namespace Genbox.FastData.Generator.Extensions;

[PublicAPI]
public static class IntegerExtensions
{
    public static string ToStringInvariant(this short value) => value.ToString(NumberFormatInfo.InvariantInfo);
    public static string ToStringInvariant(this ushort value) => value.ToString(NumberFormatInfo.InvariantInfo);
    public static string ToStringInvariant(this int value) => value.ToString(NumberFormatInfo.InvariantInfo);
    public static string ToStringInvariant(this uint value) => value.ToString(NumberFormatInfo.InvariantInfo);
    public static string ToStringInvariant(this ulong value) => value.ToString(NumberFormatInfo.InvariantInfo);
}