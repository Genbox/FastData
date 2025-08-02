using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class ConditionalCode<TKey, TValue>(ConditionalContext<TKey, TValue> ctx, SharedCode shared) : CPlusPlusOutputWriter<TKey>
{
    public override string Generate()
    {
        bool customValue = !typeof(TValue).IsPrimitive;
        StringBuilder sb = new StringBuilder();

        if (ctx.Values != null)
        {
            sb.Append($$"""
                            {{GetFieldModifier(false)}}std::array<{{GetValueTypeName(customValue)}}, {{ctx.Values.Length.ToStringInvariant()}}> values = {
                        {{FormatColumns(ctx.Values, ToValueLabel)}}
                            };

                        """);
        }

        sb.Append($$"""
                    public:
                        {{MethodAttribute}}
                        {{GetMethodModifier(true)}}bool contains(const {{KeyTypeName}} key){{PostMethodModifier}} {
                    {{GetEarlyExits(MethodType.Contains)}}
                            if ({{FormatList(ctx.Keys, x => GetEqualFunction("key", ToValueLabel(x)), " || ")}})
                                return true;

                            return false;
                        }
                    """);

        if (ctx.Values != null)
        {
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""

                            {{MethodAttribute}}
                            {{GetMethodModifier(false)}}bool try_lookup(const {{KeyTypeName}} key, const {{ValueTypeName}}*& value){{PostMethodModifier}} {
                        {{GetEarlyExits(MethodType.TryLookup)}}
                        {{GenerateBranches()}}
                                value = nullptr;
                                return false;
                            }
                        """);

            string GenerateBranches()
            {
                string ptr = customValue ? "" : "&";
                StringBuilder temp = new StringBuilder();

                for (int i = 0; i < ctx.Keys.Length; i++)
                {
                    temp.AppendLine($$"""
                                              if (key == {{ToValueLabel(ctx.Keys[i])}}) {
                                                  value = {{ptr}}values[{{i.ToStringInvariant()}}];
                                                  return true;
                                              }
                                      """);
                }

                return temp.ToString();
            }
        }

        return sb.ToString();
    }
}