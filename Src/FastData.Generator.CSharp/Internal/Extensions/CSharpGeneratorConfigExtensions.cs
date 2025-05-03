using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Generator.CSharp.Enums;
using Genbox.FastData.Helpers;
using Genbox.FastData.Specs.EarlyExit;

namespace Genbox.FastData.Generator.CSharp.Internal.Extensions;

[SuppressMessage("Major Bug", "S1244:Floating point numbers should not be tested for equality")]
[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
internal static class CSharpGeneratorConfigExtensions
{
    internal static string GetEarlyExits(this CSharpCodeGeneratorConfig cfg, GeneratorConfig genCfg)
    {
        if (cfg.GeneratorOptions.HasFlag(CSharpOptions.DisableEarlyExits))
            return string.Empty;

        StringBuilder sb = new StringBuilder();

        foreach (IEarlyExit spec in genCfg.EarlyExits)
        {
            if (spec is MinMaxLengthEarlyExit(var minLength, var maxLength))
                sb.Append(GetLengthEarlyExits(minLength, maxLength));
            else if (spec is MinMaxValueEarlyExit(var minValue, var maxValue))
                sb.Append(GetValueEarlyExits(minValue, maxValue, genCfg.DataType));
            else if (spec is LengthBitSetEarlyExit(var bitSet))
                sb.Append(GetMaskEarlyExit(bitSet));
            else
                throw new InvalidOperationException("Unknown early exit type: " + spec.GetType().Name);
        }

        return sb.ToString();
    }

    internal static string GetModFunction(this CSharpCodeGeneratorConfig config, int value)
    {
        if (value == 0)
            throw new ArgumentOutOfRangeException(nameof(value), "A length of 0 is not valid in a modulus operation");

        // x % 1 = 0
        if (value == 1)
            return "0";

        if (config.GeneratorOptions.HasFlag(CSharpOptions.DisableModulusOptimization))
            return $"hash % {value.ToString(NumberFormatInfo.InvariantInfo)}";

        if (MathHelper.IsPowerOfTwo((uint)value))
            return $"hash & {(value - 1).ToString(NumberFormatInfo.InvariantInfo)}";

        ulong multiplier = MathHelper.GetFastModMultiplier((uint)value);
        return $"unchecked((uint)((((({multiplier}ul * hash) >> 32) + 1) * {value}) >> 32))";
    }

    internal static string? GetMethodAttributes(this CSharpCodeGeneratorConfig config)
    {
        if (config.GeneratorOptions.HasFlag(CSharpOptions.DisableInlining))
            return "[MethodImpl(MethodImplOptions.NoInlining)]";

        if (config.GeneratorOptions.HasFlag(CSharpOptions.AggressiveInlining))
            return "[MethodImpl(MethodImplOptions.AggressiveInlining)]";

        return null;
    }

    internal static string GetFieldModifier(this CSharpCodeGeneratorConfig config) => config.ClassType == ClassType.Static ? "private static readonly " : "private readonly ";

    internal static string GetMethodModifier(this CSharpCodeGeneratorConfig config, bool forcePrivate = false)
    {
        string res = forcePrivate ? "private " : "public ";
        return res + (config.ClassType == ClassType.Static ? "static " : "");
    }

    internal static string GetMaskEarlyExit(ulong bitSet) =>
        $"""
                 if (({bitSet}UL & (1UL << (value.Length - 1) % 64)) == 0)
                     return false;
         """;

    internal static string GetValueEarlyExits(object min, object max, DataType dataType) =>
        $"""
                 if ({(min.Equals(max) ? $"value != {ToValueLabel(max, dataType)}" : $"value < {ToValueLabel(min, dataType)} || value > {ToValueLabel(max, dataType)}")})
                     return false;
         """;

    internal static string GetLengthEarlyExits(uint min, uint max) =>
        $"""
                 if ({(min.Equals(max) ? $"value.Length != {ToValueLabel(max)}" : $"value.Length < {ToValueLabel(min)} || value.Length > {ToValueLabel(max)}")})
                     return false;
         """;
}