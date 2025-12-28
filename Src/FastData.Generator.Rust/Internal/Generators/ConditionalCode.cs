using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Rust.Internal.Framework;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.Rust.Internal.Generators;

internal sealed class ConditionalCode<TKey, TValue>(ConditionalContext<TKey, TValue> ctx, SharedCode shared) : RustOutputWriter<TKey>
{
    public override string Generate()
    {
        bool customKey = !typeof(TKey).IsPrimitive;
        bool customType = !typeof(TValue).IsPrimitive;
        StringBuilder sb = new StringBuilder();
        ReadOnlySpan<TKey> keys = ctx.Keys.Span;
        ReadOnlySpan<TValue> values = ctx.Values.Span;

        if (!values.IsEmpty)
        {
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($"""

                          {FieldModifier}VALUES: [{GetValueTypeName(customType)}; {values.Length.ToStringInvariant()}] = [
                       {FormatColumns(values, ToValueLabel)}
                           ];
                       """);
        }

        sb.Append($$"""
                        {{MethodAttribute}}
                        {{MethodModifier}}fn contains({{InputKeyName}}: {{GetKeyTypeName(customKey)}}) -> bool {
                    {{GetMethodHeader(MethodType.Contains)}}

                            if {{FormatList(keys, x => GetEqualFunction(LookupKeyName, ToValueLabel(x)), " || ")}} {
                                return true;
                            }

                            false
                        }
                    """);

        if (!values.IsEmpty)
        {
            sb.Append($$"""

                            {{MethodAttribute}}
                            {{MethodModifier}}fn try_lookup({{InputKeyName}}: {{GetKeyTypeName(customKey)}}) -> Option<{{GetValueTypeName(customType)}}> {
                        {{GetMethodHeader(MethodType.TryLookup)}}
                        {{GenerateBranches(keys)}}
                                None
                            }
                        """);

            string GenerateBranches(ReadOnlySpan<TKey> data)
            {
                StringBuilder temp = new StringBuilder();

                for (int i = 0; i < data.Length; i++)
                {
                    temp.AppendLine($$"""
                                              if ({{GetEqualFunction(LookupKeyName, ToValueLabel(data[i]))}}) {
                                                  return Some(Self::VALUES[{{i.ToStringInvariant()}}]);
                                              }
                                      """);
                }

                return temp.ToString();
            }
        }

        return sb.ToString();
    }
}