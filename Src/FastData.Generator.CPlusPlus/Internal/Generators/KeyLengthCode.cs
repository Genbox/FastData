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
                     {{GetFieldModifier()}}std::array<{{TypeName}}, {{lengths.Length}}> entries = {
                 {{FormatColumns(lengths, ToValueLabel)}}
                     };

                 public:
                     {{GetMethodAttributes()}}
                     {{GetMethodModifier()}}bool contains(const {{TypeName}} value) noexcept
                     {
                 {{GetEarlyExits()}}

                         return {{GetEqualFunction("value", $"entries[value.length() - {ctx.MinLength.ToStringInvariant()}]")}};
                     }
                 """;
    }

    private string GenerateNormal()
    {
        List<string>?[] lengths = ctx.Lengths.Skip((int)ctx.MinLength).Take((int)((ctx.MaxLength - ctx.MinLength) + 1)).ToArray();

        return $$"""
                     {{GetFieldModifier()}}std:array<std:vector<{{TypeName}}>, {{lengths.Length}}> entries = {
                 {{FormatList(lengths, RenderMany, ",\n")}}
                     };

                 public:
                     {{GetMethodAttributes()}}
                     {{GetMethodModifier()}}bool contains(const {{TypeName}}& value) noexcept
                     {
                 {{GetEarlyExits()}}
                         std::vector<{{TypeName}}> bucket = entries[value.length() - {{ctx.MinLength}}];

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

    private string RenderMany(List<string>? x)
    {
        if (x == null)
            return "        \"\"";

        return $"new [] {{ {string.Join(",", x.Select(ToValueLabel))} }}";
    }
}