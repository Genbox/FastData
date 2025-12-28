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

        if (!ctx.Values.IsEmpty)
        {
            ReadOnlySpan<TValue> values = ctx.Values.Span;
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());
            sb.Append($"    pub const STORED_VALUE: {GetValueTypeName(customValue)} = {ToValueLabel(values[0])};");
        }

        sb.Append($$"""
                        {{MethodAttribute}}
                        {{MethodModifier}}fn contains({{InputKeyName}}: {{GetKeyTypeName(customKey)}}) -> bool {
                    {{GetMethodHeader(MethodType.Contains)}}

                            {{GetEqualFunction(LookupKeyName, ToValueLabel(ctx.Item))}}
                        }
                    """);

        if (!ctx.Values.IsEmpty)
        {
            sb.Append($$"""
                            {{MethodAttribute}}
                            {{MethodModifier}}fn try_lookup({{InputKeyName}}: {{GetKeyTypeName(customKey)}}) -> Option<{{GetValueTypeName(customValue)}}> {
                        {{GetMethodHeader(MethodType.TryLookup)}}

                                if ({{GetEqualFunction(LookupKeyName, ToValueLabel(ctx.Item))}}) {
                                    return Some(Self::STORED_VALUE);
                                }
                                None
                            }
                        """);
        }
        return sb.ToString();
    }
}