using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class BinarySearchCode<TKey, TValue>(BinarySearchContext<TKey, TValue> ctx, SharedCode shared) : RustOutputWriter<TKey>
{
    public override string Generate()
    {
        bool customKey = !typeof(TKey).IsPrimitive;
        bool customValue = !typeof(TValue).IsPrimitive;
        StringBuilder sb = new StringBuilder();

        if (ctx.Values != null)
        {
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($"""
                          {FieldModifier}VALUES: [{GetValueTypeName(customValue)}; {ctx.Values.Length.ToStringInvariant()}] = [
                       {FormatColumns(ctx.Values, ToValueLabel)}
                           ];

                       """);
        }

        sb.Append($$"""
                        {{FieldModifier}}KEYS: [{{GetKeyTypeName(customKey)}}; {{ctx.Keys.Length.ToStringInvariant()}}] = [
                    {{FormatColumns(ctx.Keys, ToValueLabel)}}
                        ];

                        {{MethodAttribute}}
                        {{MethodModifier}}fn contains(key: {{GetKeyTypeName(customKey)}}) -> bool {
                    {{GetMethodHeader(MethodType.Contains)}}

                            let mut lo: usize = 0;
                            let mut hi: usize = {{(ctx.Keys.Length - 1).ToStringInvariant()}};
                            while lo <= hi {
                                let i = lo + ((hi - lo) >> 1);
                                let entry = Self::KEYS[i];

                                if entry == {{LookupKeyName}} {
                                    return true;
                                }
                                if entry < {{LookupKeyName}} {
                                    lo = i + 1;
                                } else {
                                    hi = i - 1;
                                }
                            }

                            false
                        }
                    """);

        if (ctx.Values != null)
        {
            sb.Append($$"""

                            {{MethodAttribute}}
                            {{MethodModifier}}fn try_lookup(key: {{GetKeyTypeName(customKey)}}) -> Option<{{GetValueTypeName(customValue)}}> {
                        {{GetMethodHeader(MethodType.TryLookup)}}

                                let mut lo: usize = 0;
                                let mut hi: usize = {{(ctx.Keys.Length - 1).ToStringInvariant()}};
                                while lo <= hi {
                                    let i = lo + ((hi - lo) >> 1);
                                    let entry = Self::KEYS[i];

                                    if entry == {{LookupKeyName}} {
                                        return Some(Self::VALUES[i]);
                                    }
                                    if entry < {{LookupKeyName}} {
                                        lo = i + 1;
                                    } else {
                                        hi = i - 1;
                                    }
                                }

                                None
                            }
                        """);
        }

        return sb.ToString();
    }
}