using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class HashSetLinearCode<T>(HashSetLinearContext<T> ctx, GeneratorConfig<T> genCfg, SharedCode shared) : RustOutputWriter<T>
{
    public override string Generate()
    {
        shared.Add("linear-struct-" + genCfg.DataType, CodeType.Class, $$"""
                                                                         {{FieldModifier}}struct B {
                                                                             start_index: {{GetSmallestUnsignedType(ctx.Data.Length)}},
                                                                             end_index: {{GetSmallestUnsignedType(ctx.Data.Length)}},
                                                                         }
                                                                         """);

        return $$"""
                     {{FieldModifier}}const BUCKETS: [B; {{ctx.Buckets.Length}}] = [
                 {{FormatColumns(ctx.Buckets, static x => $"B {{ start_index: {x.StartIndex.ToStringInvariant()}, end_index: {x.EndIndex.ToStringInvariant()} }}")}}
                     ];

                     {{FieldModifier}}const ITEMS: [{{TypeNameWithLifetime}}; {{ctx.Data.Length}}] = [
                 {{FormatColumns(ctx.Data, ToValueLabel)}}
                     ];

                     {{FieldModifier}}const HASH_CODES: [u64; {{ctx.HashCodes.Length}}] = [
                 {{FormatColumns(ctx.HashCodes, static x => x.ToStringInvariant())}}
                     ];

                 {{HashSource}}

                     {{MethodAttribute}}
                     {{MethodModifier}}fn contains(value: {{TypeNameWithLifetime}}) -> bool {
                 {{EarlyExits}}

                         let hash = unsafe { Self::get_hash(value) };
                         let bucket = &Self::BUCKETS[({{GetModFunction("hash", (ulong)ctx.Buckets.Length)}}) as usize];
                         let mut index: {{GetSmallestUnsignedType(ctx.Data.Length)}} = bucket.start_index;
                         let end_index: {{GetSmallestUnsignedType(ctx.Data.Length)}} = bucket.end_index;

                         while index <= end_index {
                             if {{GetEqualFunction("Self::HASH_CODES[index as usize]", "hash")}} && {{GetEqualFunction("Self::ITEMS[index as usize]", "value")}} {
                                 return true;
                             }
                             index += 1;
                         }

                         false
                     }
                 """;
    }
}