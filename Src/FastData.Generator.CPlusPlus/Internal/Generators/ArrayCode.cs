using Genbox.FastData.Generator.Extensions;

namespace Genbox.FastData.Generator.CPlusPlus.Internal.Generators;

internal sealed class ArrayCode(GeneratorConfig genCfg, CPlusPlusCodeGeneratorConfig cfg, ArrayContext ctx) : IOutputWriter
{
    public string Generate() =>
        $$"""
              {{cfg.GetFieldModifier()}}std::array<{{genCfg.GetTypeName()}}, {{ctx.Data.Length}}> entries = {
          {{FormatColumns(ctx.Data, ToValueLabel)}}
              };

          public:
              [[nodiscard]]
              {{cfg.GetMethodModifier()}}bool contains(const {{genCfg.GetTypeName()}} value) noexcept
              {
          {{cfg.GetEarlyExits(genCfg)}}

                  for (size_t i = 0; i < {{ctx.Data.Length.ToStringInvariant()}}; i++)
                  {
                      if (entries[i] == value)
                         return true;
                  }
                  return false;
              }
          """;
}