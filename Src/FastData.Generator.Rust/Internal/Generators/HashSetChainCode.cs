using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class HashSetChainCode<T>(HashSetChainContext<T> ctx, GeneratorConfig<T> genCfg, SharedCode shared) : RustOutputWriter<T>
{
    public override string Generate()
    {
        shared.Add("chain-struct-" + genCfg.DataType, CodeType.Class, $$"""
                                                                        {{GetFieldModifier()}}struct E {
                                                                            hash_code: u32,
                                                                            next: {{GetSmallestSignedType(ctx.Buckets.Length)}},
                                                                            value: {{GetTypeNameWithLifetime()}},
                                                                        }
                                                                        """);

        return $$"""
                     {{GetFieldModifier()}}const BUCKETS: [{{GetSmallestSignedType(ctx.Buckets.Length)}}; {{ctx.Buckets.Length}}] = [
                 {{FormatColumns(ctx.Buckets, static x => x.ToStringInvariant())}}
                     ];

                     {{GetFieldModifier()}}const ENTRIES: [E; {{ctx.Entries.Length}}] = [
                 {{FormatColumns(ctx.Entries, x => $"E {{ hash_code: {x.Hash}, next: {x.Next.ToStringInvariant()}, value: {ToValueLabel(x.Value)} }}")}}
                     ];

                 {{GetHashSource()}}

                     #[must_use]
                     {{GetMethodModifier()}}fn contains(value: {{TypeName}}) -> bool {
                 {{GetEarlyExits()}}

                         let hash = unsafe { Self::get_hash(value) };
                         let index = {{GetModFunction("hash", (ulong)ctx.Buckets.Length)}};
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