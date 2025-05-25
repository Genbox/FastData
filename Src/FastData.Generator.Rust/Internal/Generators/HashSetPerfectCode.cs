using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class HashSetPerfectCode<T>(HashSetPerfectContext<T> ctx, GeneratorConfig<T> genCfg, SharedCode shared) : RustOutputWriter<T>
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
                 {{FormatColumns(ctx.Data, x => $"E {{ value: {ToValueLabel(x.Key)}, hash_code: {x.Value.ToStringInvariant()} }}")}}
                     ];

                 {{GetHashSource()}}

                     #[must_use]
                     {{GetMethodModifier()}}fn contains(value: {{TypeName}}) -> bool {
                 {{GetEarlyExits()}}
                         let hash = unsafe { Self::get_hash(value) };
                         let index = ({{GetModFunction("hash", (ulong)ctx.Data.Length)}}) as usize;
                         let entry = &Self::ENTRIES[index];

                         return hash == entry.hash_code && value == entry.value;
                     }
                 """;
    }
}