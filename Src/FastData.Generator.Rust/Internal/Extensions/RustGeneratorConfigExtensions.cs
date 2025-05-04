using System.Diagnostics.CodeAnalysis;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Enums;
using Genbox.FastData.Specs.EarlyExit;

namespace Genbox.FastData.Generator.Rust.Internal.Extensions;

[SuppressMessage("Major Bug", "S1244:Floating point numbers should not be tested for equality")]
[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
internal static class RustGeneratorConfigExtensions
{
    internal static string GetEarlyExits(this RustCodeGeneratorConfig cfg, GeneratorConfig genCfg)
    {
        if (cfg.GeneratorOptions.HasFlag(RustOptions.DisableEarlyExits))
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

    public static string? GetFieldModifier(this RustCodeGeneratorConfig config) => null;

    public static string GetMethodModifier(this RustCodeGeneratorConfig config, bool forcePrivate = false) => forcePrivate ? " " : "pub ";

    internal static string GetModFunction(this RustCodeGeneratorConfig config, int length)
    {
        return $"hash % {length.ToStringInvariant()}";
    }

    internal static string GetMaskEarlyExit(ulong bitSet) =>
        $$"""
                  if {{bitSet}}u64 & (1u64 << ((value.len() - 1) % 64)) == 0 {
                      return false;
                  }
          """;

    internal static string GetValueEarlyExits(object min, object max, DataType dataType) =>
        $$"""
                  if {{(min.Equals(max) ? $"value != {ToValueLabel(max, dataType)}" : $"value < {ToValueLabel(min, dataType)} || value > {ToValueLabel(max, dataType)}")}} {
                      return false;
                  }
          """;

    internal static string GetLengthEarlyExits(uint min, uint max) =>
        $$"""
                  if {{(min.Equals(max) ? $"value.len() != {ToValueLabel(max)}" : $"value.len() < {ToValueLabel(min)} || value.len() > {ToValueLabel(max)}")}} {
                      return false;
                  }
          """;
}