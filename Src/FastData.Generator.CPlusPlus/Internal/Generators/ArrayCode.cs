using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class ArrayCode<TKey, TValue>(ArrayContext<TKey, TValue> ctx, SharedCode shared) : CPlusPlusOutputWriter<TKey>
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
                        {{GetFieldModifier(true)}}std::array<{{KeyTypeName}}, {{ctx.Keys.Length.ToStringInvariant()}}> keys = {
                    {{FormatColumns(ctx.Keys, ToValueLabel)}}
                        };

                    public:
                        {{MethodAttribute}}
                        {{GetMethodModifier(true)}}bool contains(const {{KeyTypeName}} key){{PostMethodModifier}} {
                    {{GetEarlyExits(MethodType.Contains)}}

                            for ({{ArraySizeType}} i = 0; i < {{ctx.Keys.Length.ToStringInvariant()}}; i++)
                            {
                                if ({{GetEqualFunction("keys[i]", "key")}})
                                    return true;
                            }
                            return false;
                        }
                    """);

        if (ctx.Values != null)
        {
            string ptr = customValue ? "" : "&";
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""

                            {{MethodAttribute}}
                            {{GetMethodModifier(false)}}bool try_lookup(const {{KeyTypeName}} key, const {{ValueTypeName}}*& value){{PostMethodModifier}} {
                        {{GetEarlyExits(MethodType.TryLookup)}}

                                for ({{ArraySizeType}} i = 0; i < {{ctx.Keys.Length.ToStringInvariant()}}; i++) {
                                    if ({{GetEqualFunction("keys[i]", "key")}}) {
                                        value = {{ptr}}values[i];
                                        return true;
                                    }
                                }

                                value = nullptr;
                                return false;
                            }
                        """);
        }

        return sb.ToString();
    }
}