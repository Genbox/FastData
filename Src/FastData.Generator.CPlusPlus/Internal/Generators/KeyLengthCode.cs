using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class KeyLengthCode<TKey, TValue>(KeyLengthContext<TValue> ctx) : CPlusPlusOutputWriter<TKey>
{
    public override string Generate() => ctx.LengthsAreUniq ? GenerateUniq() : GenerateNormal();

    private string GenerateUniq()
    {
        string?[] lengths = ctx.Lengths.Skip((int)ctx.MinLength).Select(x => x?.FirstOrDefault()).ToArray();

        return $$"""
                     {{FieldModifier}}std::array<{{KeyTypeName}}, {{lengths.Length.ToStringInvariant()}}> entries = {
                 {{FormatColumns(lengths, ToValueLabel)}}
                     };

                 public:
                     {{MethodAttribute}}
                     {{MethodModifier}}bool contains(const {{KeyTypeName}} key){{PostMethodModifier}}
                     {
                 {{EarlyExits}}

                         return {{GetEqualFunction("key", $"entries[key.length() - {ctx.MinLength.ToStringInvariant()}]")}};
                     }
                 """;
    }

    private string GenerateNormal()
    {
        List<string>?[] lengths = ctx.Lengths.Skip((int)ctx.MinLength).Take((int)(ctx.MaxLength - ctx.MinLength + 1)).ToArray();

        return $$"""
                     {{FieldModifier}}std:array<std:vector<{{KeyTypeName}}>, {{lengths.Length.ToStringInvariant()}}> entries = {
                 {{FormatList(lengths, RenderMany, ",\n")}}
                     };

                 public:
                     {{MethodAttribute}}
                     {{MethodModifier}}bool contains(const {{KeyTypeName}}& key){{PostMethodModifier}}
                     {
                 {{EarlyExits}}
                         std::vector<{{KeyTypeName}}> bucket = entries[key.length() - {{ctx.MinLength.ToStringInvariant()}}];

                         if (bucket == nullptr)
                             return false;

                         foreach ({{KeyTypeName}} str in bucket)
                         {
                             if ({{GetEqualFunction("str", "key")}})
                                 return true;
                         }

                         return false;
                     }
                 """;
    }

    private string RenderMany(List<string>? x) => x == null ? "        \"\"" : $"new [] {{ {string.Join(",", x.Select(ToValueLabel))} }}";
}