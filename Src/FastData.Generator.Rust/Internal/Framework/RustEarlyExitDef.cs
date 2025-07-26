using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Definitions;
using Genbox.FastData.Generator.Rust.Enums;

namespace Genbox.FastData.Generator.Rust.Internal.Framework;

internal class RustEarlyExitDef(TypeMap map, RustOptions options) : EarlyExitDef
{
    protected override bool IsEnabled => !options.HasFlag(RustOptions.DisableEarlyExits);

    protected override string GetMaskEarlyExit(MethodType methodType, ulong bitSet) =>
        $$"""
                  if {{bitSet}}u64 & (1u64 << (key.len() - 1)) == 0 {
                      {{(methodType == MethodType.Contains ? "return false;" : "return None;" )}}
                  }
          """;

    protected override string GetValueEarlyExits<T>(MethodType methodType, T min, T max) =>
        $$"""
                  if {{(min.Equals(max) ? $"key != {map.ToValueLabel(max)}" : $"key < {map.ToValueLabel(min)} || key > {map.ToValueLabel(max)}")}} {
                      {{(methodType == MethodType.Contains ? "return false;" : "return None;" )}}
                  }
          """;

    protected override string GetLengthEarlyExits(MethodType methodType, uint min, uint max, uint minByte, uint maxByte) =>
        $$"""
                  if {{(minByte.Equals(maxByte) ? $"key.len() != {map.ToValueLabel(maxByte)} as usize" : $"key.len() < {map.ToValueLabel(minByte)} as usize || key.len() > {map.ToValueLabel(maxByte)} as usize")}} {
                      {{(methodType == MethodType.Contains ? "return false;" : "return None;" )}}
                  }
          """;
}