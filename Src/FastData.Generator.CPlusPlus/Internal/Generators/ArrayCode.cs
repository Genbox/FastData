using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class ArrayCode<TKey, TValue>(ArrayContext<TKey, TValue> ctx, SharedCode shared, string className) : CPlusPlusOutputWriter<TKey, TValue>(ctx.Values, className)
{
    public override string Generate()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($$"""
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

        if (ctx.Values != null && ObjectType != null)
        {
            //Reference types need to be initialized outside the class. For simplicity, we do it always
            shared.Add("values", CodePlacement.After, $$"""
                                                        std::array<{{TypeName}}, {{ctx.Values.Length.ToStringInvariant()}}> {{className}}::values = {
                                                        {{ValueString}}
                                                        };
                                                        """);

            if (ObjectType.IsCustomType)
                sb.AppendLine(GetObjectDeclarations(ObjectType));

            sb.Append($$"""
                            static std::array<{{TypeName}}, {{ctx.Values.Length.ToStringInvariant()}}> values;

                            {{MethodAttribute}}
                            {{MethodModifier}}bool try_lookup(const {{KeyTypeName}} key, {{ValueTypeName}}{{(ObjectType.IsCustomType ? "*&" : "")}} value){{PostMethodModifier}}
                            {
                        {{EarlyExits}}

                                for ({{ArraySizeType}} i = 0; i < {{ctx.Keys.Length.ToStringInvariant()}}; i++)
                                {
                                    if ({{GetEqualFunction("keys[i]", "key")}})
                                    {
                                        value = values[i];
                                        return true;
                                    }
                                }
                                return false;
                            }
                        """);
        }

        return sb.ToString();
    }
}