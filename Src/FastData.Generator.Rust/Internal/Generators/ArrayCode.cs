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
        bool customValue = !typeof(TValue).IsPrimitive;
        StringBuilder sb = new StringBuilder();
        ReadOnlySpan<TKey> keys = ctx.Keys.Span;

        if (!ctx.Values.IsEmpty)
        {
            ReadOnlySpan<TValue> values = ctx.Values.Span;
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($"""
                          {FieldModifier}VALUES: [{GetValueTypeName(customValue)}; {values.Length.ToStringInvariant()}] = [
                       {FormatColumns(values, ToValueLabel)}
                           ];

                       """);
        }

        sb.Append($$"""
                        {{FieldModifier}}KEYS: [{{GetKeyTypeName(customKey)}}; {{keys.Length.ToStringInvariant()}}] = [
                    {{FormatColumns(keys, ToValueLabel)}}
                        ];

                        {{MethodAttribute}}
                        {{MethodModifier}}fn contains(key: {{GetKeyTypeName(customKey)}}) -> bool {
                    {{GetMethodHeader(MethodType.Contains)}}

                            for entry in Self::KEYS.iter() {
                                if {{GetEqualFunction("*entry", LookupKeyName)}} {
                                    return true;
                                }
                            }
                            false
                        }
                    """);

        if (!ctx.Values.IsEmpty)
        {
            sb.Append($$"""

                            {{MethodAttribute}}
                            {{MethodModifier}}fn try_lookup(key: {{GetKeyTypeName(customKey)}}) -> Option<{{GetValueTypeName(customValue)}}> {
                        {{GetMethodHeader(MethodType.TryLookup)}}

                                for entry in Self::KEYS.iter() {
                                    if {{GetEqualFunction("*entry", LookupKeyName)}} {
                                        return Some(Self::VALUES[(key - 1) as usize])
                                    }
                                }
                                None
                            }
                        """);
        }
        return sb.ToString();
    }
}