using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class HashSetLinearCode<T>(GeneratorConfig<T> genCfg, RustCodeGeneratorConfig cfg, HashSetLinearContext<T> ctx, SharedCode shared) : IOutputWriter
{
    public string Generate()
    {
        shared.Add("linear-struct-" + genCfg.DataType, CodeType.Class, $$"""
                                                                         {{cfg.GetFieldModifier()}}struct B {
                                                                             start_index: {{GetSmallestUnsignedType(ctx.Data.Length)}},
                                                                             end_index: {{GetSmallestUnsignedType(ctx.Data.Length)}},
                                                                         }
                                                                         """);

        return $$"""
                     {{cfg.GetFieldModifier()}}const BUCKETS: [B; {{ctx.Buckets.Length}}] = [
                 {{FormatColumns(ctx.Buckets, static x => $"B {{ start_index: {x.StartIndex.ToStringInvariant()}, end_index: {x.EndIndex.ToStringInvariant()} }}")}}
                     ];

                     {{cfg.GetFieldModifier()}}const ITEMS: [{{genCfg.GetTypeName(true)}}; {{ctx.Data.Length}}] = [
                 {{FormatColumns(ctx.Data, ToValueLabel)}}
                     ];

                     {{cfg.GetFieldModifier()}}const HASH_CODES: [u32; {{ctx.HashCodes.Length}}] = [
                 {{FormatColumns(ctx.HashCodes, static x => x.ToStringInvariant())}}
                     ];

                 {{genCfg.GetHashSource()}}

                     #[must_use]
                     {{cfg.GetMethodModifier()}}fn contains(value: {{genCfg.GetTypeName(true)}}) -> bool {
                 {{cfg.GetEarlyExits(genCfg)}}

                         let hash = unsafe { Self::get_hash(value) };
                         let bucket = &Self::BUCKETS[({{cfg.GetModFunction(ctx.Buckets.Length)}}) as usize];
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