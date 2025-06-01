using Genbox.FastData.Generator.CPlusPlus.Internal.Framework;
using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class KeyLengthCode<T>(KeyLengthContext ctx) : CPlusPlusOutputWriter<T>
{
    public override string Generate() => ctx.LengthsAreUniq ? GenerateUniq() : GenerateNormal();

    private string GenerateUniq()
    {
        string?[] lengths = ctx.Lengths.Skip((int)ctx.MinLength).Select(x => x?.FirstOrDefault()).ToArray();

        return $$"""
                     {{FieldModifier}}std::array<{{TypeName}}, {{lengths.Length.ToStringInvariant()}}> entries = {
                 {{FormatColumns(lengths, ToValueLabel)}}
                     };

                 public:
                     {{MethodAttribute}}
                     {{MethodModifier}}bool contains(const {{TypeName}} value){{PostMethodModifier}}
                     {
                 {{EarlyExits}}

                         return {{GetEqualFunction("value", $"entries[value.length() - {ctx.MinLength.ToStringInvariant()}]")}};
                     }
                 """;
    }

    private string GenerateNormal()
    {
        List<string>?[] lengths = ctx.Lengths.Skip((int)ctx.MinLength).Take((int)(ctx.MaxLength - ctx.MinLength + 1)).ToArray();

        return $$"""
                     {{FieldModifier}}std:array<std:vector<{{TypeName}}>, {{lengths.Length.ToStringInvariant()}}> entries = {
                 {{FormatList(lengths, RenderMany, ",\n")}}
                     };

                 public:
                     {{MethodAttribute}}
                     {{MethodModifier}}bool contains(const {{TypeName}}& value){{PostMethodModifier}}
                     {
                 {{EarlyExits}}
                         std::vector<{{TypeName}}> bucket = entries[value.length() - {{ctx.MinLength.ToStringInvariant()}}];

                         if (bucket == nullptr)
                             return false;

                         foreach ({{TypeName}} str in bucket)
                         {
                             if ({{GetEqualFunction("str", "value")}})
                                 return true;
                         }

                         return false;
                     }
                 """;
    }

    private string RenderMany(List<string>? x) => x == null ? "        \"\"" : $"new [] {{ {string.Join(",", x.Select(ToValueLabel))} }}";
}