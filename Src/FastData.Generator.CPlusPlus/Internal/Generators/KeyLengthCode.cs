using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generator.Framework;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class KeyLengthCode<T>(KeyLengthContext ctx) : OutputWriter<T>
{
    public override string Generate() => ctx.LengthsAreUniq ? GenerateUniq() : GenerateNormal();

    private string GenerateUniq()
    {
        string?[] lengths = ctx.Lengths.Skip((int)ctx.MinLength).Select(x => x?.FirstOrDefault()).ToArray();

        return $$"""
                     {{GetFieldModifier()}}std::array<{{GetTypeName()}}, {{lengths.Length}}> entries = {
                 {{FormatColumns(lengths, ToValueLabel)}}
                     };

                 public:
                     {{GetMethodAttributes()}}
                     {{GetMethodModifier()}}bool contains(const {{GetTypeName()}} value) noexcept
                     {
                 {{GetEarlyExits()}}

                         return value == entries[value.length() - {{ctx.MinLength.ToStringInvariant()}}];
                     }
                 """;
    }

    private string GenerateNormal()
    {
        List<string>?[] lengths = ctx.Lengths.Skip((int)ctx.MinLength).Take((int)((ctx.MaxLength - ctx.MinLength) + 1)).ToArray();

        return $$"""
                     {{GetFieldModifier()}}std:array<std:vector<{{GetTypeName()}}>, {{lengths.Length}}> entries = {
                 {{FormatList(lengths, RenderMany, ",\n")}}
                     };

                 public:
                     {{GetMethodAttributes()}}
                     {{GetMethodModifier()}}bool contains(const {{GetTypeName()}}& value) noexcept
                     {
                 {{GetEarlyExits()}}
                         std::vector<{{GetTypeName()}}> bucket = entries[value.length() - {{ctx.MinLength}}];

                         if (bucket == nullptr)
                             return false;

                         foreach ({{GetTypeName()}} str in bucket)
                         {
                             if (str == value)
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