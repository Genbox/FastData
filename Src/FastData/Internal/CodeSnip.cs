using System.Globalization;
using System.Text;
using Genbox.FastData.Helpers;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Enums;
using Genbox.FastData.Internal.Optimization.EarlyExitSpecs;

namespace Genbox.FastData.Internal;

internal static class CodeSnip
{
    public static string GetEarlyExits(string variable, IEnumerable<IEarlyExitSpec> specs, bool forceOverride = false)
    {
        if (!forceOverride && !GlobalOptions.GenerateEarlyCondition)
            return string.Empty;

        StringBuilder sb = new StringBuilder();

        foreach (IEarlyExitSpec spec in specs)
        {
            if (spec is MinMaxLengthEarlyExitSpec(var minLength, var maxLength))
            {
                if (minLength == maxLength) //same length
                {
                    sb.Append($"""
                                      if ({variable}.Length != {maxLength})
                                          return false;
                               """);
                }
                else
                {
                    sb.Append($"""
                                       if ({variable}.Length < {minLength} || {variable}.Length > {maxLength})
                                          return false;
                               """);
                }
            }
            else if (spec is MinMaxValueEarlyExitSpec(var minValue, var maxValue))
            {
                if (minValue == maxValue) //same value
                {
                    sb.Append($"""
                                      if ({variable} != {maxValue.ToString(NumberFormatInfo.InvariantInfo)})
                                          return false;
                               """);
                }
                else
                {
                    sb.Append($"""
                                       if ({variable} < {minValue.ToString(CultureInfo.InvariantCulture)} || {variable} > {maxValue.ToString(NumberFormatInfo.InvariantInfo)})
                                          return false;
                               """);
                }
            }
        }

        return sb.ToString();
    }

    public static string GetEqualFunction(string variable1, string variable2)
    {
        return $"{variable1}.Equals({variable2})";
    }

    public static string GetCompareFunction(string variable1, string variable2)
    {
        return $"{variable1}.CompareTo({variable2})";
    }

    public static string GetSeededHashFunction32(KnownDataType type, string variable, uint seed, bool mix)
    {
        if (type == KnownDataType.String)
            return $"HashHelper.HashStringSeed({variable}, {seed})";

        //For these types, we can use identity hashing
        return type switch
        {
            KnownDataType.Char
                or KnownDataType.SByte
                or KnownDataType.Byte
                or KnownDataType.Int16
                or KnownDataType.UInt16
                or KnownDataType.Int32
                or KnownDataType.UInt32 => mix ? $"HashHelper.Mix(unchecked((uint){variable} + {seed}))" : $"unchecked((uint){variable})",
            _ => mix ? $"HashHelper.Mix(unchecked((uint){variable}.GetHashCode() + {seed}))" : $"unchecked((uint){variable}.GetHashCode())"
        };
    }

    public static string GetHashFunction32(KnownDataType type, string variable)
    {
        if (type == KnownDataType.String)
            return $"HashHelper.HashString({variable})";

        //For these types, we can use identity hashing
        return type switch
        {
            KnownDataType.Char
                or KnownDataType.SByte
                or KnownDataType.Byte
                or KnownDataType.Int16
                or KnownDataType.UInt16
                or KnownDataType.Int32
                or KnownDataType.UInt32 => $"unchecked((uint){variable})",
            _ => $"unchecked((uint){variable}.GetHashCode())"
        };
    }

    public static string GetModFunction(string variable, uint length)
    {
        if (GlobalOptions.OptimizeModulus)
        {
            if (MathHelper.IsPowerOfTwo(length))
                return $"{variable} & {(length - 1).ToString(NumberFormatInfo.InvariantInfo)}";

            ulong modMultiplier = MathHelper.GetFastModMultiplier(length);
            return $"MathHelper.FastMod({variable}, {length.ToString(NumberFormatInfo.InvariantInfo)}, {modMultiplier.ToString(NumberFormatInfo.InvariantInfo)})";
        }

        return $"{variable} % {length.ToString(NumberFormatInfo.InvariantInfo)}";
    }

    public static string? GetMethodAttributes()
    {
        return GlobalOptions.DisableInlining ? "[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]" : null;
    }

    public static string ToValueLabel(object value)
    {
        return value switch
        {
            string val => $"\"{val}\"",
            char val => $"'{val}'",
            bool val => val.ToString().ToLowerInvariant(),
            IFormattable val => val.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }
}