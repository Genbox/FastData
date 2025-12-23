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
        ReadOnlySpan<TKey> keys = ctx.Keys.Span;
        ReadOnlySpan<TValue> values = ctx.Values.Span;

        if (!values.IsEmpty)
        {
            sb.Append($$"""
                            {{GetFieldModifier(false)}}std::array<{{GetValueTypeName(customValue)}}, {{values.Length.ToStringInvariant()}}> values = {
                        {{FormatColumns(values, ToValueLabel)}}
                            };

                        """);
        }

        sb.Append($$"""
                    public:
                        {{MethodAttribute}}
                        {{GetMethodModifier(true)}}bool contains(const {{KeyTypeName}} key){{PostMethodModifier}} {
                    {{GetMethodHeader(MethodType.Contains)}}
                            if ({{FormatList(keys, x => GetEqualFunction(LookupKeyName, ToValueLabel(x)), " || ")}})
                                return true;

                            return false;
                        }
                    """);

        if (!values.IsEmpty)
        {
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""

                            {{MethodAttribute}}
                            {{GetMethodModifier(false)}}bool try_lookup(const {{KeyTypeName}} key, const {{ValueTypeName}}*& value){{PostMethodModifier}} {
                        {{GetMethodHeader(MethodType.TryLookup)}}
                        {{GenerateBranches(keys)}}
                                value = nullptr;
                                return false;
                            }
                        """);

            string GenerateBranches(ReadOnlySpan<TKey> data)
            {
                string ptr = customValue ? "" : "&";
                StringBuilder temp = new StringBuilder();

                for (int i = 0; i < data.Length; i++)
                {
                    temp.AppendLine($$"""
                                              if ({{GetEqualFunction(LookupKeyName, ToValueLabel(data[i]))}}) {
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