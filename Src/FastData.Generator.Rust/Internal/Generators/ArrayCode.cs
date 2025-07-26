using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class ArrayCode<TKey, TValue>(ArrayContext<TKey, TValue> ctx, SharedCode shared) : RustOutputWriter<TKey>
{
    public override string Generate()
    {
        bool customKey = !typeof(TKey).IsPrimitive;
        StringBuilder sb = new StringBuilder();

        sb.Append($$"""
                        {{FieldModifier}}const KEYS: [{{GetKeyTypeName(customKey)}}; {{ctx.Keys.Length.ToStringInvariant()}}] = [
                    {{FormatColumns(ctx.Keys, ToValueLabel)}}
                        ];

                        {{MethodAttribute}}
                        {{MethodModifier}}fn contains(key: {{GetKeyTypeName(customKey)}}) -> bool {
                    {{GetEarlyExits(MethodType.Contains)}}

                            for entry in Self::KEYS.iter() {
                                if {{GetEqualFunction("*entry", "key")}} {
                                    return true;
                                }
                            }
                            false
                        }
                    """);

        if (ctx.Values != null)
        {
            bool customValue = !typeof(TValue).IsPrimitive;

            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            shared.Add(CodePlacement.Before, $"""

                                                 {FieldModifier} static VALUES: [{GetValueTypeName(customValue)}; {ctx.Values.Length.ToStringInvariant()}] = [
                                              {FormatColumns(ctx.Values, ToValueLabel)}
                                                  ];
                                              """);

            sb.Append($$"""

                            {{MethodAttribute}}
                            {{MethodModifier}}fn try_lookup(key: {{GetKeyTypeName(customKey)}}) -> Option<{{GetValueTypeName(customValue)}}> {
                        {{GetEarlyExits(MethodType.TryLookup)}}

                                for entry in Self::KEYS.iter() {
                                    if {{GetEqualFunction("*entry", "key")}} {
                                        return Some(VALUES[(key - 1) as usize])
                                    }
                                }
                                None
                            }
                        """);
        }
        return sb.ToString();
    }
}