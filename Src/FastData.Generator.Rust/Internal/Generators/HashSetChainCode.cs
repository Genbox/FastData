using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class HashSetChainCode(GeneratorConfig genCfg, RustCodeGeneratorConfig cfg, HashSetChainContext ctx, SharedCode shared) : IOutputWriter
{
    public string Generate()
    {
        shared.Add("chain-struct-" + genCfg.DataType, CodeType.Class, $$"""
                                                                        {{cfg.GetFieldModifier()}}struct E {
                                                                            hash_code: u32,
                                                                            next: {{GetSmallestSignedType(ctx.Buckets.Length)}},
                                                                            value: {{genCfg.GetTypeName(true)}},
                                                                        }
                                                                        """);

        return $$"""
                     {{cfg.GetFieldModifier()}}const BUCKETS: [{{GetSmallestSignedType(ctx.Buckets.Length)}}; {{ctx.Buckets.Length}}] = [
                 {{FormatColumns(ctx.Buckets, static x => x.ToStringInvariant())}}
                     ];

                     {{cfg.GetFieldModifier()}}const ENTRIES: [E; {{ctx.Entries.Length}}] = [
                 {{FormatColumns(ctx.Entries, static x => $"E {{ hash_code: {x.Hash}, next: {x.Next.ToStringInvariant()}, value: {ToValueLabel(x.Value)} }}")}}
                     ];

                 {{genCfg.GetHashSource()}}

                     #[must_use]
                     {{cfg.GetMethodModifier()}}fn contains(value: {{genCfg.GetTypeName()}}) -> bool {
                 {{cfg.GetEarlyExits(genCfg)}}

                         let hash = unsafe { Self::get_hash(value) };
                         let index = {{cfg.GetModFunction(ctx.Buckets.Length)}};
                         let mut i: {{GetSmallestSignedType(ctx.Buckets.Length)}} = (Self::BUCKETS[index as usize] as {{GetSmallestSignedType(ctx.Buckets.Length)}}) - 1;

                         while i >= 0 {
                             let entry = &Self::ENTRIES[i as usize];
                             if entry.hash_code == hash && entry.value == value {
                                 return true;
                             }
                             i = entry.next;
                         }

                         false
                     }
                 """;
    }
}