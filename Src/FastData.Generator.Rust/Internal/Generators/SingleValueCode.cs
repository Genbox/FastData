using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class SingleValueCode<TKey, TValue>(SingleValueContext<TKey, TValue> ctx, SharedCode shared) : RustOutputWriter<TKey>
{
    public override string Generate()
    {
        bool customKey = !typeof(TKey).IsPrimitive;
        bool customValue = !typeof(TValue).IsPrimitive;
        StringBuilder sb = new StringBuilder();

        if (ctx.Values != null)
        {
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());
            sb.Append($"    pub const STORED_VALUE: {GetValueTypeName(customValue)} = {ToValueLabel(ctx.Values[0])};");
        }

        sb.Append($$"""
                        {{MethodAttribute}}
                        {{MethodModifier}}fn contains(key: {{GetKeyTypeName(customKey)}}) -> bool {
                            {{GetEqualFunction("key", ToValueLabel(ctx.Item))}}
                        }
                    """);

        if (ctx.Values != null)
        {
            sb.Append($$"""
                            {{MethodAttribute}}
                            {{MethodModifier}}fn try_lookup(key: {{GetKeyTypeName(customKey)}}) -> Option<{{GetValueTypeName(customValue)}}> {
                                if ({{GetEqualFunction("key", ToValueLabel(ctx.Item))}}) {
                                    return Some(Self::STORED_VALUE);
                                }
                                None
                            }
                        """);
        }
        return sb.ToString();
    }
}