using Genbox.FastData.Generator.Enums;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class PerfectHashBruteForceCode<T>(GeneratorConfig genCfg, RustCodeGeneratorConfig cfg, PerfectHashBruteForceContext<T> ctx, SharedCode shared) : IOutputWriter
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
                 {{FormatColumns(ctx.Data, static x => $"E {{ value: {ToValueLabel(x.Key)}, hash_code: {ToValueLabel(x.Value)} }}")}}
                     ];

                 {{genCfg.GetHashSource()}}

                     fn murmur_32(mut h: u32) -> u32 {
                         h ^= h >> 16;
                         h = h.wrapping_mul(0x85EB_CA6B);
                         h ^= h >> 13;
                         h = h.wrapping_mul(0xC2B2_AE35);
                         h ^= h >> 16;
                         h
                     }

                     #[must_use]
                     {{cfg.GetMethodModifier()}}fn contains(value: {{genCfg.GetTypeName()}}) -> bool {
                 {{cfg.GetEarlyExits(genCfg)}}
                         let hash = Self::murmur_32(unsafe { Self::get_hash(value) } ^ {{ctx.Seed}});
                         let index = ({{cfg.GetModFunction(ctx.Data.Length)}}) as usize;
                         let entry = &Self::ENTRIES[index];

                         return hash == entry.hash_code && value == entry.value;
                     }
                 """;
    }
}