using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class HashSetChainCode<T>(HashSetChainContext<T> ctx, GeneratorConfig<T> genCfg, SharedCode shared) : RustOutputWriter<T>
{
    public override string Generate()
    {
        shared.Add("chain-struct-" + genCfg.DataType, CodeType.Class, $$"""
                                                                        {{FieldModifier}}struct E {
                                                                            {{(ctx.StoreHashCode ? $"hash_code: {HashSizeType}," : "")}}
                                                                            next: {{GetSmallestSignedType(ctx.Buckets.Length)}},
                                                                            value: {{TypeNameWithLifetime}},
                                                                        }
                                                                        """);

        return $$"""
                     {{FieldModifier}}const BUCKETS: [{{GetSmallestSignedType(ctx.Buckets.Length)}}; {{ctx.Buckets.Length.ToStringInvariant()}}] = [
                 {{FormatColumns(ctx.Buckets, static x => x.ToStringInvariant())}}
                     ];

                     {{FieldModifier}}const ENTRIES: [E; {{ctx.Entries.Length}}] = [
                 {{FormatColumns(ctx.Entries, x => $"E {{ {(ctx.StoreHashCode ? $"hash_code: {x.Hash}, " : "")}next: {x.Next.ToStringInvariant()}, value: {ToValueLabel(x.Value)} }}")}}
                     ];

                 {{HashSource}}

                     {{MethodAttribute}}
                     {{MethodModifier}}fn contains(value: {{TypeName}}) -> bool {
                 {{EarlyExits}}

                         let hash = unsafe { Self::get_hash(value) };
                         let index = {{GetModFunction("hash", (ulong)ctx.Buckets.Length)}};
                         let mut i: {{GetSmallestSignedType(ctx.Buckets.Length)}} = (Self::BUCKETS[index as usize] as {{GetSmallestSignedType(ctx.Buckets.Length)}}) - 1;

                         while i >= 0 {
                             let entry = &Self::ENTRIES[i as usize];
                             if {{(ctx.StoreHashCode ? GetEqualFunction("entry.hash_code", "hash") + " && " : "")}}{{GetEqualFunction("entry.value", "value")}} {
                                 return true;
                             }
                             i = entry.next;
                         }

                         false
                     }
                 """;
    }
}