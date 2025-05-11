using Genbox.FastData.Generator.CSharp.Internal.Framework;
using Genbox.FastData.Generator.Enums;
using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.CSharp.Internal.Generators;

internal sealed class KeyLengthCode<T>(KeyLengthContext ctx, CSharpCodeGeneratorConfig cfg) : CSharpOutputWriter<T>(cfg)
{
    //TODO: Remove gaps in array by reducing the index via a map (if (idx > 10) return 4) where 4 is the number to subtract from the index

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
              {{GetFieldModifier()}}{{TypeName}}[] _entries = new {{TypeName}}[] {
          {{FormatColumns(ctx.Lengths.Skip((int)ctx.MinLength).Select(x => x?.FirstOrDefault()), ToValueLabel)}}
              };

              {{GetMethodAttributes()}}
              {{GetMethodModifier()}}bool Contains({{TypeName}} value)
              {
          {{GetEarlyExits()}}

                  return {{GetEqualFunction("value", $"_entries[value.Length - {ctx.MinLength.ToStringInvariant()}]")}};
              }
          """;

    private string GenerateUniqSwitch() =>
        $$"""
              {{GetMethodAttributes()}}
              {{GetMethodModifier()}}bool Contains({{TypeName}} value)
              {
          {{GetEarlyExits()}}

                  switch (value.Length)
                  {
          {{FormatList(ctx.Lengths.Where(x => x != null).Select(x => x![0]), x => $"""
                                                                                               case {x.Length}:
                                                                                                   return {GetEqualFunction("value", ToValueLabel(x))};
                                                                                   """)}}
                      default:
                          return false;
                  }
              }
          """;

    private string GenerateNormal() =>
        $$"""
              {{GetFieldModifier()}}{{TypeName}}[]?[] _entries = new {{TypeName}}[]?[] {
          {{FormatList(ctx.Lengths.Skip((int)ctx.MinLength).Take((int)((ctx.MaxLength - ctx.MinLength) + 1)), RenderMany, ",\n")}}
              };

              {{GetMethodAttributes()}}
              {{GetMethodModifier()}}bool Contains({{TypeName}} value)
              {
          {{GetEarlyExits()}}
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

    private string RenderMany(List<string>? x)
    {
        if (x == null)
            return "        null";

        return $"new [] {{{string.Join(",", x.Select(ToValueLabel))}}}";
    }
}