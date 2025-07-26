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
        StringBuilder sb = new StringBuilder();

        sb.Append($$"""
                        {{FieldModifier}}const KEYS: [{{GetKeyTypeName(customKey)}}; {{ctx.Keys.Length.ToStringInvariant()}}] = [
                    {{FormatColumns(ctx.Keys, ToValueLabel)}}
                        ];

                        {{MethodAttribute}}
                        {{MethodModifier}}fn contains(key: {{GetKeyTypeName(customKey)}}) -> bool {
                    {{GetEarlyExits(MethodType.Contains)}}

                            let mut lo: usize = 0;
                            let mut hi: usize = {{(ctx.Keys.Length - 1).ToStringInvariant()}};
                            while lo <= hi {
                                let i = lo + ((hi - lo) >> 1);
                                let entry = Self::KEYS[i];

                                if entry == key {
                                    return true;
                                }
                                if entry < key {
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

                                let mut lo: usize = 0;
                                let mut hi: usize = {{(ctx.Keys.Length - 1).ToStringInvariant()}};
                                while lo <= hi {
                                    let i = lo + ((hi - lo) >> 1);
                                    let entry = Self::KEYS[i];

                                    if entry == key {
                                        return Some(VALUES[i]);
                                    }
                                    if entry < key {
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