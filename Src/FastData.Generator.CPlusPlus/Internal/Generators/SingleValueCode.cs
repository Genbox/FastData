using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class SingleValueCode<TKey, TValue>(SingleValueContext<TKey, TValue> ctx, SharedCode shared) : CPlusPlusOutputWriter<TKey>
{
    public override string Generate()
    {
        bool customValue = !typeof(TValue).IsPrimitive;
        StringBuilder sb = new StringBuilder();

        if (ctx.Values != null)
            sb.Append($"      static inline const auto stored_value = {ToValueLabel(ctx.Values[0])};");

        sb.Append($$"""
                    public:
                        {{MethodAttribute}}
                        {{GetMethodModifier(true)}}bool contains(const {{KeyTypeName}} key){{PostMethodModifier}} {
                            return {{GetEqualFunction("key", ToValueLabel(ctx.Item))}};
                        }
                    """);

        if (ctx.Values != null)
        {
            string ptr = customValue ? "" : "&";
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""

                            {{MethodAttribute}}
                            {{GetMethodModifier(false)}}bool try_lookup(const {{KeyTypeName}} key, const {{ValueTypeName}}*& value){{PostMethodModifier}} {
                                if ({{GetEqualFunction("key", ToValueLabel(ctx.Item))}}) {
                                    value = {{ptr}}stored_value;
                                    return true;
                                }

                                value = nullptr;
                                return false;
                            }
                        """);
        }
        return sb.ToString();
    }
}