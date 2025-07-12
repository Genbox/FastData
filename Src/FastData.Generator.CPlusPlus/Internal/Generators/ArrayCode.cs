using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class ArrayCode<TKey, TValue>(ArrayContext<TKey, TValue> ctx, SharedCode shared, string className) : CPlusPlusOutputWriter<TKey>
{
    public override string Generate()
    {
        if (ctx.ValueSpec != null)
        {
            string typeName = ctx.ValueSpec.IsCustomType ? $"{className}::{ValueTypeName}" : ValueTypeName;

            //Reference types need to be initialized outside the class. For simplicity, we do it always
            shared.Add("values", CodePlacement.After, $$"""
                                                        std::array<{{typeName}}, {{ctx.ValueSpec.Values.Length.ToStringInvariant()}}> {{className}}::values = {
                                                        {{(ctx.ValueSpec.IsCustomType ? PrintValues(ctx.ValueSpec) : FormatColumns(ctx.ValueSpec.Values, ToValueLabel))}}
                                                        };
                                                        """);

            return $$"""
                     {{GetObjectDeclaration(ctx.ValueSpec)}}
                         static std::array<{{ValueTypeName}}, {{ctx.ValueSpec.Values.Length.ToStringInvariant()}}> values;

                         {{FieldModifier}}std::array<{{KeyTypeName}}, {{ctx.Keys.Length.ToStringInvariant()}}> keys = {
                     {{FormatColumns(ctx.Keys, ToValueLabel)}}
                         };

                     public:
                         {{MethodAttribute}}
                         {{MethodModifier}}bool try_lookup(const {{KeyTypeName}} key, {{ValueTypeName}}*& value){{PostMethodModifier}}
                         {
                     {{EarlyExits}}

                             for ({{ArraySizeType}} i = 0; i < {{ctx.Keys.Length.ToStringInvariant()}}; i++)
                             {
                                 if ({{GetEqualFunction("keys[i]", "key")}})
                                 {
                                     value = &values[i];
                                     return true;
                                 }
                             }
                             return false;
                         }

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
                     """;
        }

        return $$"""
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
                 """;
    }
}