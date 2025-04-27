using Genbox.FastData.Contexts.Misc;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class HashSetLinearCode(GeneratorConfig genCfg, RustGeneratorConfig cfg, HashSetLinearContext ctx, SharedCode shared) : IOutputWriter
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
                 {{FormatColumns(ctx.Buckets, RenderBucket)}}
                     ];

                     {{cfg.GetFieldModifier()}}const ITEMS: [{{genCfg.GetTypeName(true)}}; {{ctx.Data.Length}}] = [
                 {{FormatColumns(ctx.Data, static (sb, x) => sb.Append(ToValueLabel(x)))}}
                     ];

                     {{cfg.GetFieldModifier()}}const HASH_CODES: [u32; {{ctx.HashCodes.Length}}] = [
                 {{FormatColumns(ctx.HashCodes, static (sb, obj) => sb.Append(obj))}}
                     ];

                 {{genCfg.GetHashSource()}}

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

    private static void RenderBucket(StringBuilder sb, HashSetBucket x) =>
        sb.Append("B { start_index: ").Append(x.StartIndex).Append(", end_index: ").Append(x.EndIndex).Append(" }");
}