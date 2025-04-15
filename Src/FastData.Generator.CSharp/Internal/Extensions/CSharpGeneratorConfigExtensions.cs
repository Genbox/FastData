using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Helpers;
using Genbox.FastData.Specs.EarlyExit;

namespace Genbox.FastData.Generator.CSharp.Internal.Extensions;

[SuppressMessage("Major Bug", "S1244:Floating point numbers should not be tested for equality")]
[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
internal static class CSharpGeneratorConfigExtensions
{
    internal static string GetEarlyExits(this CSharpGeneratorConfig cfg, GeneratorConfig genCfg)
    {
        if (cfg.GeneratorOptions.HasFlag(CSharpOptions.DisableEarlyExits))
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

    internal static string GetModFunction(this CSharpGeneratorConfig config, int length)
    {
        if (length == 0)
            throw new ArgumentOutOfRangeException(nameof(length), "A length of 0 is not valid in a modulus operation");

        // x % 1 = 0
        if (length == 1)
            return "0";

        if (config.GeneratorOptions.HasFlag(CSharpOptions.DisableModulusOptimization))
            return $"hash % {length.ToString(NumberFormatInfo.InvariantInfo)}";

        if (MathHelper.IsPowerOfTwo((uint)length))
            return $"hash & {(length - 1).ToString(NumberFormatInfo.InvariantInfo)}";

        ulong modMultiplier = MathHelper.GetFastModMultiplier((uint)length);
        return $"MathHelper.FastMod(hash, {length.ToString(NumberFormatInfo.InvariantInfo)}, {modMultiplier.ToString(NumberFormatInfo.InvariantInfo)})";
    }

    internal static string? GetMethodAttributes(this CSharpGeneratorConfig config)
    {
        if (config.GeneratorOptions.HasFlag(CSharpOptions.DisableInlining))
            return "[MethodImpl(MethodImplOptions.NoInlining)]";

        if (config.GeneratorOptions.HasFlag(CSharpOptions.AggressiveInlining))
            return "[MethodImpl(MethodImplOptions.AggressiveInlining)]";

        return null;
    }

    internal static string? GetModifier(this CSharpGeneratorConfig config) => config.ClassType == ClassType.Static ? " static" : null;

    internal static string GetMaskEarlyExit(ulong bitSet)
        => $"""
                   if (({bitSet}UL & (1UL << (value.Length - 1) % 64)) == 0)
                       return false;
            """;

    internal static string GetValueEarlyExits<T>(T min, T max, bool length) where T : IFormattable
        => min.Equals(max)
            ? $"""
                      if (value{(length ? ".Length" : "")} != {max})
                          return false;
               """
            : $"""
                       if (value{(length ? ".Length" : "")} < {min} || value{(length ? ".Length" : "")} > {max})
                          return false;
               """;
}