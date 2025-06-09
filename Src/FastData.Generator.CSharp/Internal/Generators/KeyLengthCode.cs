using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class KeyLengthCode<T>(KeyLengthContext<T> ctx, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<T>(cfg)
{
    public override string Generate(ReadOnlySpan<T> data)
    {
        if (ctx.LengthsAreUniq)
        {
            //There is an assumptions, that when LengthsAreUniq is true, all the length buckets only contain one item
            return cfg.KeyLengthUniqBranchType switch
            {
                BranchType.If => GenerateUniqIf(),
                BranchType.Switch => GenerateUniqSwitch(),
                _ => throw new InvalidOperationException("Invalid branch type: " + cfg.KeyLengthUniqBranchType)
            };
        }

        return GenerateNormal();
    }

    private string GenerateUniqIf() =>
        $$"""
              {{FieldModifier}}{{TypeName}}[] _entries = new {{TypeName}}[] {
          {{FormatColumns(ctx.Lengths.AsReadOnlySpan((int)ctx.MinLength), x => ToValueLabel(x?.FirstOrDefault()))}}
              };

              {{MethodAttribute}}
              {{MethodModifier}}bool Contains({{TypeName}} value)
              {
          {{EarlyExits}}

                  return {{GetEqualFunction("value", $"_entries[value.Length - {ctx.MinLength.ToStringInvariant()}]")}};
              }
          """;

    private string GenerateUniqSwitch()
    {
        string[] filtered = ctx.Lengths.Where(x => x != null).Select(x => x![0]).ToArray();

        return $$"""
                     {{MethodAttribute}}
                     {{MethodModifier}}bool Contains({{TypeName}} value)
                     {
                 {{EarlyExits}}

                         switch (value.Length)
                         {
                 {{FormatList(filtered, x => $"""
                                                          case {x.Length}:
                                                              return {GetEqualFunction("value", ToValueLabel(x))};
                                              """)}}
                             default:
                                 return false;
                         }
                     }
                 """;
    }

    private string GenerateNormal()
    {
        return $$"""
                     {{FieldModifier}}{{TypeName}}[]?[] _entries = new {{TypeName}}[]?[] {
                 {{FormatList(ctx.Lengths.AsReadOnlySpan((int)ctx.MinLength, (int)(ctx.MaxLength - ctx.MinLength + 1)), RenderMany, ",\n")}}
                     };

                     {{MethodAttribute}}
                     {{MethodModifier}}bool Contains({{TypeName}} value)
                     {
                 {{EarlyExits}}
                         {{TypeName}}[]? bucket = _entries[value.Length - {{ctx.MinLength}}];

                         if (bucket == null)
                             return false;

                         foreach ({{TypeName}} str in bucket)
                         {
                             if ({{GetEqualFunction("value", "str")}})
                                 return true;
                         }

                         return false;
                     }
                 """;
    }

    private string RenderMany(List<string>? x) => x == null ? "        null" : $"new [] {{{string.Join(",", x.Select(ToValueLabel))}}}";
}