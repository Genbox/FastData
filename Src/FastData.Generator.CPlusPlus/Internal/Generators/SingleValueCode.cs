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

        if (!ctx.Values.IsEmpty)
        {
            ReadOnlySpan<TValue> values = ctx.Values.Span;
            sb.Append($"      static inline const auto stored_value = {ToValueLabel(values[0])};");
        }

        sb.Append($$"""
                    public:
                        {{MethodAttribute}}
                        {{GetMethodModifier(true)}}bool contains(const {{KeyTypeName}} {{InputKeyName}}){{PostMethodModifier}} {
                    {{GetMethodHeader(MethodType.Contains)}}

                            return {{GetEqualFunction(LookupKeyName, ToValueLabel(ctx.Item))}};
                        }
                    """);

        if (!ctx.Values.IsEmpty)
        {
            string ptr = customValue ? "" : "&";
            shared.Add(CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""

                            {{MethodAttribute}}
                            {{GetMethodModifier(false)}}bool try_lookup(const {{KeyTypeName}} {{InputKeyName}}, const {{ValueTypeName}}*& value){{PostMethodModifier}} {
                        {{GetMethodHeader(MethodType.TryLookup)}}

                                if ({{GetEqualFunction(LookupKeyName, ToValueLabel(ctx.Item))}}) {
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