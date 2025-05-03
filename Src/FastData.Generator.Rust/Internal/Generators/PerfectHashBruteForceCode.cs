namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class PerfectHashBruteForceCode(GeneratorConfig genCfg, RustCodeGeneratorConfig cfg, PerfectHashBruteForceContext ctx, SharedCode shared) : IOutputWriter
{
    public string Generate()
    {
        shared.Add("ph-struct-" + genCfg.DataType, CodeType.Class, $$"""
                                                                     {{cfg.GetFieldModifier()}}struct E {
                                                                         value: {{genCfg.GetTypeName(true)}},
                                                                         hash_code: u32,
                                                                     }
                                                                     """);

        return $$"""
                     {{cfg.GetFieldModifier()}}const ENTRIES: [E; {{ctx.Data.Length}}] = [
                 {{FormatColumns(ctx.Data, Render)}}
                     ];

                 {{genCfg.GetHashSource()}}

                     fn murmur_32(mut h: u32) -> u32 {
                         h ^= h >> 16;
                         h = h.wrapping_mul(0x85EB_CA6B);
                         h ^= h >> 13;
                         h = h.wrapping_mul(0xC2B2_AE35);
                         h ^= h >> 16;
                         return h;
                     }

                     {{cfg.GetMethodModifier()}}fn contains(value: {{genCfg.GetTypeName()}}) -> bool {
                 {{cfg.GetEarlyExits(genCfg)}}
                         let hash = Self::murmur_32(unsafe { Self::get_hash(value) } ^ {{ctx.Seed}});
                         let index = ({{cfg.GetModFunction(ctx.Data.Length)}}) as usize;
                         let entry = &Self::ENTRIES[index];

                         return hash == entry.hash_code && value == entry.value;
                     }
                 """;
    }

    private static void Render(StringBuilder sb, KeyValuePair<object, uint> obj) =>
        sb.Append("E { value: ").Append(ToValueLabel(obj.Key)).Append(", hash_code: ").Append(ToValueLabel(obj.Value)).Append(" }");
}