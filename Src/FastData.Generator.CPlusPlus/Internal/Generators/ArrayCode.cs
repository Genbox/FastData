using System.Globalization;
using Genbox.FastData.Abstracts;
using Genbox.FastData.Configs;
using Genbox.FastData.Contexts;
using Genbox.FastData.Generator.CPlusPlus.Internal.Extensions;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class ArrayCode(GeneratorConfig genCfg, CPlusPlusGeneratorConfig cfg, ArrayContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              {{cfg.GetFieldModifier()}} std::array<{{genCfg.GetTypeName()}}, {{ctx.Data.Length}}> entries = {
          {{FormatColumns(ctx.Data, static (sb, x) => sb.Append(ToValueLabel(x)))}}
              };

          public:
              {{cfg.GetMethodModifier()}} bool contains(const {{genCfg.GetTypeName()}}& value)
              {
          {{cfg.GetEarlyExits(genCfg)}}

                  for (int i = 0; i < {{ctx.Data.Length.ToString(NumberFormatInfo.InvariantInfo)}}; i++)
                  {
                      if ({{genCfg.GetEqualFunction("entries[i]")}})
                         return true;
                  }
                  return false;
              }
          """;
}