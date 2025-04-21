namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class PerfectHashBruteForceCode(GeneratorConfig genCfg, RustGeneratorConfig cfg, PerfectHashBruteForceContext ctx) : IOutputWriter
{
    public string Generate()
    {
        SharedCode.Instance.Add("ph-struct-" + genCfg.DataType, CodeType.Class, $$"""
                                                                                  {{cfg.GetFieldModifier()}}struct E {
                                                                                      value: {{genCfg.GetTypeName(true)}},
                                                                                      hash_code: u32,
                                                                                  }
                                                                                  """);

        return $$"""
                     {{cfg.GetFieldModifier()}}const ENTRIES: [E; {{ctx.Data.Length}}] = [
                 {{FormatColumns(ctx.Data, Render)}}
                     ];

                 {{genCfg.GetHashSource(true)}}

                     {{cfg.GetMethodModifier()}}fn contains(value: {{genCfg.GetTypeName()}}) -> bool {
                 {{cfg.GetEarlyExits(genCfg)}}

                         let hash = Self::get_hash(value, {{ctx.Seed}});
                         let index = ({{cfg.GetModFunction(ctx.Data.Length)}}) as usize;
                         let entry = &Self::ENTRIES[index];

                         hash == entry.hash_code && {{genCfg.GetEqualFunction("entry.value")}}
                     }
                 """;
    }

    private static void Render(StringBuilder sb, KeyValuePair<object, uint> obj) => sb.Append("E { value: ").Append(ToValueLabel(obj.Key)).Append(", hash_code: ").Append(ToValueLabel(obj.Value)).Append(" }");
}