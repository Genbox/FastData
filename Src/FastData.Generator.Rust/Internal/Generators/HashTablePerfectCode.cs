using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class HashTablePerfectCode<TKey, TValue>(HashTablePerfectContext<TKey, TValue> ctx, GeneratorConfig<TKey> genCfg, SharedCode shared) : RustOutputWriter<TKey>
{
    public override string Generate()
    {
        if (ctx.StoreHashCode)
        {
            shared.Add("ph-struct-" + genCfg.DataType, CodePlacement.After, $$"""
                                                                         {{FieldModifier}}struct E {
                                                                             key: {{TypeNameWithLifetime}},
                                                                             hash_code: {{HashSizeType}},
                                                                         }
                                                                         """);

            return $$"""
                         {{FieldModifier}}const ENTRIES: [E; {{ctx.Data.Length.ToStringInvariant()}}] = [
                     {{FormatColumns(ctx.Data, x => $"E {{ key: {ToValueLabel(x.Key)}, hash_code: {x.Value.ToStringInvariant()} }}")}}
                         ];

                     {{HashSource}}

                         {{MethodAttribute}}
                         {{MethodModifier}}fn contains(key: {{KeyTypeName}}) -> bool {
                     {{EarlyExits}}
                             let hash = unsafe { Self::get_hash(key) };
                             let index = ({{GetModFunction("hash", (ulong)ctx.Data.Length)}}) as usize;
                             let entry = &Self::ENTRIES[index];

                             return {{GetEqualFunction("hash", "entry.hash_code")}} && {{GetEqualFunction("key", "entry.key")}};
                         }
                     """;
        }

        return $$"""
                     {{FieldModifier}}const ENTRIES: [{{TypeNameWithLifetime}}; {{ctx.Data.Length.ToStringInvariant()}}] = [
                 {{FormatColumns(ctx.Data, x => ToValueLabel(x.Key))}}
                     ];

                 {{HashSource}}

                     {{MethodAttribute}}
                     {{MethodModifier}}fn contains(key: {{KeyTypeName}}) -> bool {
                 {{EarlyExits}}
                         let hash = unsafe { Self::get_hash(key) };
                         let index = ({{GetModFunction("hash", (ulong)ctx.Data.Length)}}) as usize;

                         return {{GetEqualFunction("key", "Self::ENTRIES[index]")}};
                     }
                 """;
    }
}