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

                            let mut lo: usize = 0;
                            let mut hi: usize = {{(keys.Length - 1).ToStringInvariant()}};
                            while lo <= hi {
                                let i = lo + ((hi - lo) >> 1);
                                let entry = Self::KEYS[i];
                                let order = {{GetCompareFunction("entry", LookupKeyName)}};

                                if order == 0 {
                                    return true;
                                }
                                if order < 0 {
                                    lo = i + 1;
                                } else {
                                    hi = i - 1;
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

                                let mut lo: usize = 0;
                                let mut hi: usize = {{(keys.Length - 1).ToStringInvariant()}};
                                while lo <= hi {
                                    let i = lo + ((hi - lo) >> 1);
                                    let entry = Self::KEYS[i];
                                    let order = {{GetCompareFunction("entry", LookupKeyName)}};

                                    if order == 0 {
                                        return Some(Self::VALUES[i]);
                                    }
                                    if order < 0 {
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