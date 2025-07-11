using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class HashSetPerfectCode<T>(HashSetPerfectContext<T> ctx, GeneratorConfig<T> genCfg, SharedCode shared) : RustOutputWriter<T>
{
    public override string Generate()
    {
        if (ctx.StoreHashCode)
        {
            shared.Add("ph-struct-" + genCfg.DataType, CodeType.Class, $$"""
                                                                         {{FieldModifier}}struct E {
                                                                             value: {{TypeNameWithLifetime}},
                                                                             hash_code: {{HashSizeType}},
                                                                         }
                                                                         """);

            return $$"""
                         {{FieldModifier}}const ENTRIES: [E; {{ctx.Data.Length.ToStringInvariant()}}] = [
                     {{FormatColumns(ctx.Data, x => $"E {{ value: {ToValueLabel(x.Key)}, hash_code: {x.Value.ToStringInvariant()} }}")}}
                         ];

                     {{HashSource}}

                         {{MethodAttribute}}
                         {{MethodModifier}}fn contains(value: {{TypeName}}) -> bool {
                     {{EarlyExits}}
                             let hash = unsafe { Self::get_hash(value) };
                             let index = ({{GetModFunction("hash", (ulong)ctx.Data.Length)}}) as usize;
                             let entry = &Self::ENTRIES[index];

                             return {{GetEqualFunction("hash", "entry.hash_code")}} && {{GetEqualFunction("value", "entry.value")}};
                         }
                     """;
        }

        return $$"""
                     {{FieldModifier}}const ENTRIES: [{{TypeNameWithLifetime}}; {{ctx.Data.Length.ToStringInvariant()}}] = [
                 {{FormatColumns(ctx.Data, x => ToValueLabel(x.Key))}}
                     ];

                 {{HashSource}}

                     {{MethodAttribute}}
                     {{MethodModifier}}fn contains(value: {{TypeName}}) -> bool {
                 {{EarlyExits}}
                         let hash = unsafe { Self::get_hash(value) };
                         let index = ({{GetModFunction("hash", (ulong)ctx.Data.Length)}}) as usize;

                         return {{GetEqualFunction("value", "Self::ENTRIES[index]")}};
                     }
                 """;
    }
}