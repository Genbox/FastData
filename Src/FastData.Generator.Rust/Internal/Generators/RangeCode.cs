using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class RangeCode<TKey, TValue>(RangeContext<TKey, TValue> ctx, SharedCode shared) : RustOutputWriter<TKey>
{
    public override string Generate()
    {
        bool customKey = !typeof(TKey).IsPrimitive;
        bool customValue = !typeof(TValue).IsPrimitive;
        StringBuilder sb = new StringBuilder();

        if (ctx.Values != null)
        {
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""
                           {{FieldModifier}}VALUES: [{{GetValueTypeName(customValue)}}; {{ctx.Values.Length.ToStringInvariant()}}] = [
                        {{FormatColumns(ctx.Values, ToValueLabel)}}
                            ];

                        """);
        }

        sb.Append($$"""
                        {{MethodAttribute}}
                        {{MethodModifier}}fn contains(key: {{GetKeyTypeName(customKey)}}) -> bool {
                            return key >= Self::MIN_KEY && key <= Self::MAX_KEY;
                        }
                    """);

        if (ctx.Values != null)
        {
            sb.Append($$"""

                            {{MethodAttribute}}
                            {{MethodModifier}}fn try_lookup(key: {{GetKeyTypeName(customKey)}}) -> Option<{{GetValueTypeName(customValue)}}> {
                                if key < Self::MIN_KEY || key > Self::MAX_KEY {
                                    return None;
                                }

                                let index = {{GetIndexExpression()}};
                                Some(Self::VALUES[index])
                            }
                        """);
        }

        return sb.ToString();
    }

    private string GetIndexExpression() => GeneratorConfig.KeyType switch
    {
        KeyType.Char => "(key as u32 - Self::MIN_KEY as u32) as usize",
        KeyType.SByte or KeyType.Int16 or KeyType.Int32 or KeyType.Int64 => "((key as i64) - (Self::MIN_KEY as i64)) as usize",
        KeyType.Byte or KeyType.UInt16 or KeyType.UInt32 or KeyType.UInt64 => "((key as u64) - (Self::MIN_KEY as u64)) as usize",
        _ => "(key - Self::MIN_KEY) as usize"
    };
}