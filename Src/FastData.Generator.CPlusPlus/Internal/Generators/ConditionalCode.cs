using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class ConditionalCode<TKey, TValue>(ConditionalContext<TKey, TValue> ctx, SharedCode shared, string className) : CPlusPlusOutputWriter<TKey, TValue>(ctx.Values)
{
    public override string Generate()
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($$"""
                        public:
                            {{MethodAttribute}}
                            {{MethodModifier}}bool contains(const {{KeyTypeName}} key){{PostMethodModifier}}
                            {
                        {{EarlyExits}}

                                if ({{FormatList(ctx.Keys, x => GetEqualFunction("key", ToValueLabel(x)), " || ")}})
                                    return true;

                                return false;
                            }
                        """);

        if (ctx.Values != null && ObjectType != null)
        {
            shared.Add("values", CodePlacement.After, $$"""
                                                        std::array<{{TypeName}}, {{ctx.Values.Length.ToStringInvariant()}}> {{className}}::values = {
                                                        {{ValueString}}
                                                        };
                                                        """);

            if (ObjectType.IsCustomType)
                shared.Add("classes", CodePlacement.Before, GetObjectDeclarations(ObjectType));

            sb.Append($$"""
                            static std::array<{{TypeName}}, {{ctx.Values.Length.ToStringInvariant()}}> values;

                            {{MethodAttribute}}
                            {{MethodModifier}}bool try_lookup(const {{KeyTypeName}} key, const {{ValueTypeName}}*& value){{PostMethodModifier}}
                            {
                        {{EarlyExits}}

                        {{GenerateBranches()}}

                                value = nullptr;
                                return false;
                            }
                        """);

            string GenerateBranches()
            {
                StringBuilder temp = new StringBuilder();

                for (int i = 0; i < ctx.Keys.Length; i++)
                {
                    temp.AppendLine($$"""
                                              if (key == {{ToValueLabel(ctx.Keys[i])}})
                                              {
                                                  value = {{(ObjectType.IsCustomType ? "" : "&")}}values[{{i.ToStringInvariant()}}];
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