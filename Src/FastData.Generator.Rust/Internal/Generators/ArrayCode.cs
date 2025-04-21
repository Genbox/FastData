namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class ArrayCode(GeneratorConfig genCfg, RustGeneratorConfig cfg, ArrayContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              {{cfg.GetFieldModifier()}}const ENTRIES: [{{genCfg.GetTypeName()}}; {{ctx.Data.Length}}] = [
          {{FormatColumns(ctx.Data, static (sb, x) => sb.Append(ToValueLabel(x)))}}
              ];

              {{cfg.GetMethodModifier()}}fn contains(value: {{genCfg.GetTypeName()}}) -> bool {
          {{cfg.GetEarlyExits(genCfg)}}

                  for entry in Self::ENTRIES.iter() {
                      if {{genCfg.GetEqualFunction("entry", true)}} {
                          return true;
                      }
                  }
                  false
              }
          """;
}