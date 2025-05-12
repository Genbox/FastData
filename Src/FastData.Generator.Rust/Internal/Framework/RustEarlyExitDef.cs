using Genbox.FastData.Generator.Framework;
using Genbox.FastData.Generator.Framework.Definitions;
using Genbox.FastData.Generator.Rust.Enums;

namespace Genbox.FastData.Generator.Rust.Internal.Framework;

internal class RustEarlyExitDef(TypeHelper helper, RustOptions options) : EarlyExitDef
{
    protected override bool IsEnabled => !options.HasFlag(RustOptions.DisableEarlyExits);

    protected override string GetMaskEarlyExit(ulong bitSet) =>
        $$"""
                  if {{bitSet}}u64 & (1u64 << ((value.len() - 1) % 64)) == 0 {
                      return false;
                  }
          """;

    protected override string GetValueEarlyExits<T>(T min, T max) =>
        $$"""
                  if {{(min.Equals(max) ? $"value != {helper.ToValueLabel(max)}" : $"value < {helper.ToValueLabel(min)} || value > {helper.ToValueLabel(max)}")}} {
                      return false;
                  }
          """;

    protected override string GetLengthEarlyExits(uint min, uint max) =>
        $$"""
                  if {{(min.Equals(max) ? $"value.len() != {helper.ToValueLabel(max)} as usize" : $"value.len() < {helper.ToValueLabel(min)} as usize || value.len() > {helper.ToValueLabel(max)} as usize")}} {
                      return false;
                  }
          """;
}