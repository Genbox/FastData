using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class SingleValueCode<TKey, TValue>(SingleValueContext<TKey, TValue> ctx, SharedCode shared) : CPlusPlusOutputWriter<TKey>
{
    public override string Generate()
    {
        StringBuilder sb = new StringBuilder();

        sb.Append($$"""
                    public:
                        {{MethodAttribute}}
                        {{MethodModifier}}bool contains(const {{KeyTypeName}} key){{PostMethodModifier}}
                        {
                            return {{GetEqualFunction("key", ToValueLabel(ctx.Item))}};
                        }
                    """);

        if (ctx.Values != null)
        {
            shared.Add("classes", CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""

                            static inline const auto stored_value = {{ToValueLabel(ctx.Values[0])}};

                            {{MethodAttribute}}
                            {{MethodModifier}}bool try_lookup(const {{KeyTypeName}} key, const {{ValueTypeName}}*& value){{PostMethodModifier}}
                            {
                                if ({{GetEqualFunction("key", ToValueLabel(ctx.Item))}})
                                {
                                    value = stored_value;
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