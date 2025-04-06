using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.EarlyExitSpecs;
using Genbox.FastData.Helpers;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Extensions;

[SuppressMessage("Major Bug", "S1244:Floating point numbers should not be tested for equality")]
[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
internal static class CSharpGeneratorConfigExtensions
{
    internal static string GetEarlyExits(this CPlusPlusGeneratorConfig cfg, GeneratorConfig genCfg)
    {
        if (cfg.GeneratorOptions.HasFlag(CPlusPlusOptions.DisableEarlyExits))
            return string.Empty;

        StringBuilder sb = new StringBuilder();

        foreach (IEarlyExit spec in genCfg.EarlyExits)
        {
            if (spec is MinMaxLengthEarlyExit(var minLength, var maxLength))
                sb.Append(GetValueEarlyExits(minLength, maxLength, true));
            else if (spec is MinMaxValueEarlyExit(var minValue, var maxValue))
                sb.Append(GetValueEarlyExits(minValue, maxValue, false));
            else if (spec is MinMaxUnsignedValueEarlyExit(var minUValue, var maxUValue))
                sb.Append(GetValueEarlyExits(minUValue, maxUValue, false));
            else if (spec is MinMaxFloatValueEarlyExit(var minFloatValue, var maxFloatValue))
                sb.Append(GetValueEarlyExits(minFloatValue, maxFloatValue, false));
            else if (spec is LengthBitSetEarlyExit(var bitSet))
                sb.Append(GetMaskEarlyExit(bitSet));
            else
                throw new InvalidOperationException("Unknown early exit type: " + spec.GetType().Name);
        }

        return sb.ToString();
    }

    public static string GetFieldModifier(this CPlusPlusGeneratorConfig config) => "inline static const";

    public static string GetMethodModifier(this CPlusPlusGeneratorConfig config) => "static";

    internal static string GetModFunction(this CPlusPlusGeneratorConfig config, int length)
    {
        return $"hash % {length.ToString(NumberFormatInfo.InvariantInfo)}";
    }

    internal static string GetMaskEarlyExit(ulong bitSet)
        => $"""
                   if (({bitSet}ULL & (1ULL << (value.length() - 1) % 64)) == 0)
                       return false;
            """;

    internal static string GetValueEarlyExits<T>(T min, T max, bool length) where T : IFormattable
        => min.Equals(max)
            ? $"""
                      if (const size_t len = value.length(); len != {max})
                          return false;
               """
            : $"""
                       if (const size_t len = value.length(); len < {min} || len > {max})
                           return false;
               """;
}