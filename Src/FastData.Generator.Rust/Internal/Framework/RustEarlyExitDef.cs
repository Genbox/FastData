using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Definitions;
using Genbox.FastData.Generator.Rust.Enums;

namespace Genbox.FastData.Generator.Rust.Internal.Framework;

internal class RustEarlyExitDef(TypeHelper helper, RustOptions options) : EarlyExitDef
{
    protected override bool IsEnabled => !options.HasFlag(RustOptions.DisableEarlyExits);

    protected override string GetMaskEarlyExit(ulong bitSet) =>
        $$"""
                  if {{bitSet}}u64 & (1u64 << (value.len() - 1)) == 0 {
                      return false;
                  }
          """;

    protected override string GetValueEarlyExits<T>(T min, T max) =>
        $$"""
                  if {{(min.Equals(max) ? $"value != {helper.ToValueLabel(max)}" : $"value < {helper.ToValueLabel(min)} || value > {helper.ToValueLabel(max)}")}} {
                      return false;
                  }
          """;

    protected override string GetLengthEarlyExits(uint min, uint max, uint minByte, uint maxByte) =>
        $$"""
                  if {{(minByte.Equals(maxByte) ? $"value.len() != {helper.ToValueLabel(maxByte)} as usize" : $"value.len() < {helper.ToValueLabel(minByte)} as usize || value.len() > {helper.ToValueLabel(maxByte)} as usize")}} {
                      return false;
                  }
          """;
}