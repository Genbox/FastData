using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class HashSetLinearCode<T>(HashSetLinearContext<T> ctx, GeneratorConfig<T> genCfg, SharedCode shared) : RustOutputWriter<T>
{
    public override string Generate()
    {
        shared.Add("linear-struct-" + genCfg.DataType, CodeType.Class, $$"""
                                                                         {{GetFieldModifier()}}struct B {
                                                                             start_index: {{GetSmallestUnsignedType(ctx.Data.Length)}},
                                                                             end_index: {{GetSmallestUnsignedType(ctx.Data.Length)}},
                                                                         }
                                                                         """);

        return $$"""
                     {{GetFieldModifier()}}const BUCKETS: [B; {{ctx.Buckets.Length}}] = [
                 {{FormatColumns(ctx.Buckets, static x => $"B {{ start_index: {x.StartIndex.ToStringInvariant()}, end_index: {x.EndIndex.ToStringInvariant()} }}")}}
                     ];

                     {{GetFieldModifier()}}const ITEMS: [{{GetTypeNameWithLifetime()}}; {{ctx.Data.Length}}] = [
                 {{FormatColumns(ctx.Data, ToValueLabel)}}
                     ];

                     {{GetFieldModifier()}}const HASH_CODES: [{{HashType}}; {{ctx.HashCodes.Length}}] = [
                 {{FormatColumns(ctx.HashCodes, static x => x.ToStringInvariant())}}
                     ];

                 {{GetHashSource()}}

                     #[must_use]
                     {{GetMethodModifier()}}fn contains(value: {{GetTypeNameWithLifetime()}}) -> bool {
                 {{GetEarlyExits()}}

                         let hash = unsafe { Self::get_hash(value) };
                         let bucket = &Self::BUCKETS[({{GetModFunction("hash", (ulong)ctx.Buckets.Length)}}) as usize];
                         let mut index: {{GetSmallestUnsignedType(ctx.Data.Length)}} = bucket.start_index;
                         let end_index: {{GetSmallestUnsignedType(ctx.Data.Length)}} = bucket.end_index;

                         while index <= end_index {
                             if Self::HASH_CODES[index as usize] == hash && Self::ITEMS[index as usize] == value {
                                 return true;
                             }
                             index += 1;
                         }

                         false
                     }
                 """;
    }
}