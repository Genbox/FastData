using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Rust.Internal.Framework;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class PerfectHashBruteForceCode<T>(PerfectHashBruteForceContext<T> ctx, GeneratorConfig<T> genCfg, SharedCode shared) : RustOutputWriter<T>
{
    public override string Generate()
    {
        shared.Add("ph-struct-" + genCfg.DataType, CodeType.Class, $$"""
                                                                     {{GetFieldModifier()}}struct E {
                                                                         value: {{GetTypeNameWithLifetime()}},
                                                                         hash_code: u32,
                                                                     }
                                                                     """);

        return $$"""
                     {{GetFieldModifier()}}const ENTRIES: [E; {{ctx.Data.Length}}] = [
                 {{FormatColumns(ctx.Data, x => $"E {{ value: {ToValueLabel(x.Key)}, hash_code: {ToValueLabel(x.Value)} }}")}}
                     ];

                 {{GetHashSource()}}

                     fn murmur_32(mut h: u32) -> u32 {
                         h ^= h >> 16;
                         h = h.wrapping_mul(0x85EB_CA6B);
                         h ^= h >> 13;
                         h = h.wrapping_mul(0xC2B2_AE35);
                         h ^= h >> 16;
                         h
                     }

                     #[must_use]
                     {{GetMethodModifier()}}fn contains(value: {{TypeName}}) -> bool {
                 {{GetEarlyExits()}}
                         let hash = Self::murmur_32(unsafe { Self::get_hash(value) } ^ {{ctx.Seed}});
                         let index = ({{GetModFunction("hash", (ulong)ctx.Data.Length)}}) as usize;
                         let entry = &Self::ENTRIES[index];

                         return hash == entry.hash_code && value == entry.value;
                     }
                 """;
    }
}