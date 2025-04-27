using Genbox.FastData.Contexts.Misc;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class HashSetChainCode(GeneratorConfig genCfg, RustGeneratorConfig cfg, HashSetChainContext ctx, SharedCode shared) : IOutputWriter
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
                 {{FormatColumns(ctx.Buckets, static (sb, x) => sb.Append(x))}}
                     ];

                     {{cfg.GetFieldModifier()}}const ENTRIES: [E; {{ctx.Entries.Length}}] = [
                 {{FormatColumns(ctx.Entries, RenderEntry)}}
                     ];

                 {{genCfg.GetHashSource()}}

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

    private static void RenderEntry(StringBuilder sb, HashSetEntry x) =>
        sb.Append("E { hash_code: ").Append(x.Hash).Append(", next: ").Append(x.Next).Append(", value: ").Append(ToValueLabel(x.Value)).Append(" }");
}