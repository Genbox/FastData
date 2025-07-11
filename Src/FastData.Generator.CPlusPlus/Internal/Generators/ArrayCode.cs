using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class ArrayCode<TKey, TValue>(ArrayContext<TKey, TValue> ctx) : CPlusPlusOutputWriter<TKey>
{
    public override string Generate()
    {
        if (ctx.Values != null)
            return $$"""
                         {{FieldModifier}}std::array<{{ValueTypeName}}, {{ctx.Values.Length.ToStringInvariant()}}> values = {
                     {{FormatColumns(ctx.Values, ToValueLabel)}}
                         };

                         {{FieldModifier}}std::array<{{KeyTypeName}}, {{ctx.Keys.Length.ToStringInvariant()}}> keys = {
                     {{FormatColumns(ctx.Keys, ToValueLabel)}}
                         };

                     public:
                         {{MethodAttribute}}
                         {{MethodModifier}}{{ValueTypeName}} get_value(const {{KeyTypeName}} value){{PostMethodModifier}}
                         {
                     {{EarlyExits}}

                             for ({{ArraySizeType}} i = 0; i < {{ctx.Keys.Length.ToStringInvariant()}}; i++)
                             {
                                 if ({{GetEqualFunction("keys[i]", "value")}})
                                    return values[i];
                             }
                             return NULL;
                         }

                         {{MethodAttribute}}
                         {{MethodModifier}}bool contains(const {{KeyTypeName}} value){{PostMethodModifier}}
                         {
                     {{EarlyExits}}

                             for ({{ArraySizeType}} i = 0; i < {{ctx.Keys.Length.ToStringInvariant()}}; i++)
                             {
                                 if ({{GetEqualFunction("keys[i]", "value")}})
                                    return true;
                             }
                             return false;
                         }
                     """;

        return $$"""
                 {{FieldModifier}}std::array<{{KeyTypeName}}, {{ctx.Keys.Length.ToStringInvariant()}}> keys = {
                 {{FormatColumns(ctx.Keys, ToValueLabel)}}
                 };

                 public:
                     {{MethodAttribute}}
                     {{MethodModifier}}bool contains(const {{KeyTypeName}} value){{PostMethodModifier}}
                     {
                 {{EarlyExits}}

                         for ({{ArraySizeType}} i = 0; i < {{ctx.Keys.Length.ToStringInvariant()}}; i++)
                         {
                             if ({{GetEqualFunction("keys[i]", "value")}})
                                return true;
                         }
                         return false;
                     }
                 """;
    }
}