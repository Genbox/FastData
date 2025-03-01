using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Genbox.FastData.Enums;
using Genbox.FastData.Helpers;
using Genbox.FastData.Internal.Abstracts;
using Genbox.FastData.Internal.Enums;
using Genbox.FastData.Internal.Optimization.EarlyExitSpecs;

namespace Genbox.FastData.Internal;

internal static class CodeSnip
{
    [SuppressMessage("Major Bug", "S1244:Floating point numbers should not be tested for equality")]
    [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
    public static string GetEarlyExits(string variable, IEnumerable<IEarlyExit> specs, bool forceOverride = false)
    {
        if (!forceOverride && !GlobalOptions.GenerateEarlyCondition)
            return string.Empty;

        StringBuilder sb = new StringBuilder();

        foreach (IEarlyExit spec in specs)
        {
            if (spec is MinMaxLengthEarlyExit(var minLength, var maxLength))
                sb.Append(GetValueEarlyExits(variable, minLength, maxLength, true));
            else if (spec is MinMaxValueEarlyExit(var minValue, var maxValue))
                sb.Append(GetValueEarlyExits(variable, minValue, maxValue, false));
            else if (spec is MinMaxUnsignedValueEarlyExit(var minUValue, var maxUValue))
                sb.Append(GetValueEarlyExits(variable, minUValue, maxUValue, false));
            else if (spec is MinMaxFloatValueEarlyExit(var minFloatValue, var maxFloatValue))
                sb.Append(GetValueEarlyExits(variable, minFloatValue, maxFloatValue, false));
            else if (spec is LengthBitSetEarlyExit(var bitSet))
                sb.Append(GetMaskEarlyExit(variable, bitSet));
            else
                throw new InvalidOperationException("Unknown early exit type: " + spec.GetType().Name);
        }

        return sb.ToString();
    }

    private static string GetMaskEarlyExit(string variable, ulong bitSet)
        => $"""
                   if (({bitSet}UL & 1UL << ({variable}.Length -1) & 63) == 0)
                       return false;
            """;

    private static string GetValueEarlyExits<T>(string variable, T min, T max, bool length) where T : IFormattable
        => min.Equals(max)
            ? $"""
                      if ({variable}{(length ? ".Length" : "")} != {max})
                          return false;
               """
            : $"""
                       if ({variable}{(length ? ".Length" : "")} < {min} || {variable}{(length ? ".Length" : "")} > {max})
                          return false;
               """;

    public static string GetEqualFunction(KnownDataType type, string variable1, string variable2)
    {
        if (type == KnownDataType.String)
            return $"StringComparer.Ordinal.Equals({variable1}, {variable2})";

        return $"{variable1}.Equals({variable2})";
    }

    public static string GetCompareFunction(string variable1, string variable2)
    {
        return $"{variable1}.CompareTo({variable2})";
    }

    public static string GetSeededHashFunction32(KnownDataType type, string variable, uint seed)
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
                or KnownDataType.UInt32 => $"unchecked((uint)({variable} ^ {seed}))",
            _ => $"unchecked((uint)({variable}.GetHashCode() ^ {seed}))"
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

    public static string GetModifier(ClassType ct) => ct == ClassType.Static ? " static" : "";

    public static string JoinValues<T>(T[] data, Action<StringBuilder, T> render, string delim = ", ")
    {
        return JoinValues(data.AsSpan(), render, delim);
    }

    public static string JoinValues<T>(Span<T> data, Action<StringBuilder, T> render, string delim = ", ")
    {
        StringBuilder sb = new StringBuilder();

        int i = 0;
        foreach (T obj in data)
        {
            render(sb, obj);

            if (i != data.Length - 1)
                sb.Append(delim);

            i++;
        }

        return sb.ToString();
    }
}