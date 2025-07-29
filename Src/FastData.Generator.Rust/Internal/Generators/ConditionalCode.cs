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

        if (ctx.Values != null)
        {
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($"""

                          {FieldModifier}VALUES: [{GetValueTypeName(customType)}; {ctx.Values.Length.ToStringInvariant()}] = [
                       {FormatColumns(ctx.Values, ToValueLabel)}
                           ];
                       """);
        }

        sb.Append($$"""
                        {{MethodAttribute}}
                        {{MethodModifier}}fn contains(key: {{GetKeyTypeName(customKey)}}) -> bool {
                    {{GetEarlyExits(MethodType.Contains)}}

                            if {{FormatList(ctx.Keys, x => GetEqualFunction("key", ToValueLabel(x)), " || ")}} {
                                return true;
                            }

                            false
                        }
                    """);

        if (ctx.Values != null)
        {
            sb.Append($$"""

                            {{MethodAttribute}}
                            {{MethodModifier}}fn try_lookup(key: {{GetKeyTypeName(customKey)}}) -> Option<{{GetValueTypeName(customType)}}> {
                        {{GetEarlyExits(MethodType.TryLookup)}}
                        {{GenerateBranches()}}
                                None
                            }
                        """);

            string GenerateBranches()
            {
                StringBuilder temp = new StringBuilder();

                for (int i = 0; i < ctx.Keys.Length; i++)
                {
                    temp.AppendLine($$"""
                                              if (key == {{ToValueLabel(ctx.Keys[i])}}) {
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