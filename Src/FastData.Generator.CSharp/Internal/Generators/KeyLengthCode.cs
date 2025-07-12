using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;
using Genbox.FastData.Generators.Contexts;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class KeyLengthCode<TKey, TValue>(KeyLengthContext<TValue> ctx, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<TKey>(cfg)
{
    public override string Generate()
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
              {{FieldModifier}}{{KeyTypeName}}[] _entries = new {{KeyTypeName}}[] {
          {{FormatColumns(ctx.Lengths.AsReadOnlySpan((int)ctx.MinLength), x => ToValueLabel(x?.FirstOrDefault()))}}
              };

              {{MethodAttribute}}
              {{MethodModifier}}bool Contains({{KeyTypeName}} key)
              {
          {{EarlyExits}}

                  return {{GetEqualFunction("key", $"_entries[key.Length - {ctx.MinLength.ToStringInvariant()}]")}};
              }
          """;

    private string GenerateUniqSwitch()
    {
        string[] filtered = ctx.Lengths.Where(x => x != null).Select(x => x![0]).ToArray();

        return $$"""
                     {{MethodAttribute}}
                     {{MethodModifier}}bool Contains({{KeyTypeName}} key)
                     {
                 {{EarlyExits}}

                         switch (key.Length)
                         {
                 {{FormatList(filtered, x => $"""
                                                          case {x.Length}:
                                                              return {GetEqualFunction("key", ToValueLabel(x))};
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
                     {{FieldModifier}}{{KeyTypeName}}[]?[] _entries = new {{KeyTypeName}}[]?[] {
                 {{FormatList(ctx.Lengths.AsReadOnlySpan((int)ctx.MinLength, (int)(ctx.MaxLength - ctx.MinLength + 1)), RenderMany, ",\n")}}
                     };

                     {{MethodAttribute}}
                     {{MethodModifier}}bool Contains({{KeyTypeName}} key)
                     {
                 {{EarlyExits}}
                         {{KeyTypeName}}[]? bucket = _entries[key.Length - {{ctx.MinLength}}];

                         if (bucket == null)
                             return false;

                         foreach ({{KeyTypeName}} str in bucket)
                         {
                             if ({{GetEqualFunction("key", "str")}})
                                 return true;
                         }

                         return false;
                     }
                 """;
    }

    private string RenderMany(List<string>? x) => x == null ? "        null" : $"new [] {{{string.Join(",", x.Select(ToValueLabel))}}}";
}