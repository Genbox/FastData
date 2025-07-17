using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class ArrayCode<TKey, TValue>(ArrayContext<TKey, TValue> ctx, SharedCode shared) : CPlusPlusOutputWriter<TKey>
{
    public override string Generate()
    {
        bool customType = !typeof(TValue).IsPrimitive;
        StringBuilder sb = new StringBuilder();

        sb.Append($$"""
                    {{FieldModifier}}std::array<{{KeyTypeName}}, {{ctx.Keys.Length.ToStringInvariant()}}> keys = {
                    {{FormatColumns(ctx.Keys, ToValueLabel)}}
                    };

                    public:
                        {{MethodAttribute}}
                        {{MethodModifier}}bool contains(const {{KeyTypeName}} key){{PostMethodModifier}}
                        {
                    {{EarlyExits}}

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
            shared.Add("classes", CodePlacement.Before, GetObjectDeclarations<TValue>());

            sb.Append($$"""

                            static inline std::array<{{ValueTypeName}}{{(customType ? "*" : "")}}, {{ctx.Values.Length.ToStringInvariant()}}> values = {
                            {{FormatColumns(ctx.Values, ToValueLabel)}}
                            };

                            {{MethodAttribute}}
                            {{MethodModifier}}bool try_lookup(const {{KeyTypeName}} key, const {{ValueTypeName}}*& value){{PostMethodModifier}}
                            {
                        {{EarlyExits}}

                                for ({{ArraySizeType}} i = 0; i < {{ctx.Keys.Length.ToStringInvariant()}}; i++)
                                {
                                    if ({{GetEqualFunction("keys[i]", "key")}})
                                    {
                                        value = {{(customType ? "" : "&")}}values[i];
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