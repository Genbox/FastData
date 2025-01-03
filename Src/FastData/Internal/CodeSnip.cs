using System.Globalization;
using System.Text;
using Genbox.FastData.Helpers;
using Genbox.FastData.Internal.Abstracts;
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

    public static string GetHashFunction(string variable, uint seed)
    {
        return $"unchecked((uint)HashHelper.Hash({variable}, {seed}))";
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